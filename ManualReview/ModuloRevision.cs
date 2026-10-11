using SistemaBancario.DataStructures;
using SistemaBancario.Models;
using SistemaBancario.Persistence;
using SistemaBancario.Security;

namespace SistemaBancario.Services
{
    public class ModuloRevision
    {
        private readonly List<TransaccionRevision> _bandejaRevisiones;
        private readonly List<RegistroRechazoCliente> _listaRechazosClientes;
        private readonly HashSet<string> _idsProcesados; // Garantiza unicidad estricta y evita duplicidad
        private readonly ColaDinamicaPrioritaria _colaEmisionSwift; // COLA 3: Salida SWIFT Reordenada Dinámicamente

        public ModuloRevision(
            List<TransaccionRevision> bandejaRevisiones,
            List<RegistroRechazoCliente> listaRechazosClientes,
            HashSet<string> idsProcesados,
            ColaDinamicaPrioritaria colaEmisionSwift)
        {
            _bandejaRevisiones = bandejaRevisiones ?? throw new ArgumentNullException(nameof(bandejaRevisiones));
            _listaRechazosClientes = listaRechazosClientes ?? throw new ArgumentNullException(nameof(listaRechazosClientes));
            _idsProcesados = idsProcesados ?? throw new ArgumentNullException(nameof(idsProcesados));
            _colaEmisionSwift = colaEmisionSwift ?? throw new ArgumentNullException(nameof(colaEmisionSwift));
        }

        public string AprobarTransaccion(string transactionId, string usuarioId, UserRole rolUsuario, Transaction txOriginal)
        {
            if (!AuthManager.PuedeAprobarObservadas())
            {
                return "❌ Acceso denegado por Segregación de Funciones: El perfil Administrador no tiene permitido intervenir en la aprobación de transferencias.";
            }

            var revision = _bandejaRevisiones.Find(r => r.TransactionId == transactionId);
            if (revision == null) return "❌ Error: Transacción no encontrada en la bandeja de revisión manual.";

            if (revision.EstadoRevision != "Pendiente")
                return $"⚠️ Acción rechazada: La transacción ya fue procesada como '{revision.EstadoRevision}'.";

            if (_idsProcesados.Contains(transactionId))
                return $"❌ Error de atomicidad: El ID {transactionId} ya cuenta con una resolución.";

            DateTime timestampUtc = ConfiguracionOperativa.ObtenerFechaHoraActual();

            // 1. Actualizar orden de revisión
            revision.EstadoRevision = "Aprobado";
            revision.EvaluadoPorEjecutivoId = usuarioId;
            revision.EvaluationTimestamp = timestampUtc;
            revision.MotivoRevision += $" | APROBADO por [{rolUsuario}] {usuarioId}";

            // 2. Mapear estado operativo
            txOriginal.Status = (rolUsuario == UserRole.Manager) ? "ApprovedByManager" : "ApprovedByExecutive";
            txOriginal.ApprovedByUserId = usuarioId;
            txOriginal.ApprovalTimestamp = timestampUtc;

            // 3. Sincronizar actualización en log de observadas
            ArchivoManager.ActualizarEstadoOperacionObservada(transactionId, "Processed", timestampUtc, $"Aprobado por [{rolUsuario}] {usuarioId}");

            // 4. EMISIÓN INTERNA SWIFT
            string tramaSwift = txOriginal.GenerarSwift(timestampUtc);
            bool estaEnVentana = GestorTransacciones.EsVentanaAtencionSwift(timestampUtc);

            if (estaEnVentana)
            {
                txOriginal.EstadoTransmision = "TransmitidoEfectivo";
                txOriginal.SwiftTransmisionEfectivaTimestamp = timestampUtc;
                txOriginal.Status = GestorTransacciones.DeterminarEstadoConSwift(txOriginal.Status);
                ArchivoManager.RegistrarSwiftSalida(txOriginal, tramaSwift, timestampUtc, esEmisionEjecutivo: true, timestampEjecutivo: timestampUtc);
            }
            else
            {
                txOriginal.EstadoTransmision = "EncoladoParaTransmision";
                txOriginal.SwiftTransmisionEfectivaTimestamp = GestorTransacciones.CalcularFechaEmisionSwift(timestampUtc);
                _colaEmisionSwift.EncolarConPrioridad(txOriginal, txOriginal.PrioridadAsignada);
                ArchivoManager.RegistrarSwiftSalida(txOriginal, tramaSwift, timestampUtc, esEmisionEjecutivo: true, timestampEjecutivo: timestampUtc);
            }

            // 5. Persistir en Log de Decisiones
            ArchivoManager.RegistrarDecisionEjecutivo(
                transactionId: transactionId,
                sourceAccount: txOriginal.SourceAccountNumber,
                amount: txOriginal.Amount,
                currency: txOriginal.Currency,
                destinationCurrency: txOriginal.DestinationCurrency,
                ejecutivoId: usuarioId,
                estadoDecision: "Aprobada",
                detalleMotivo: $"Aprobación manual emitida por [{rolUsuario}] | Estado Transmisión: {txOriginal.EstadoTransmision}",
                timestampDecision: timestampUtc,
                prioridadAsignada: txOriginal.PrioridadAsignada
            );

            _idsProcesados.Add(transactionId);
            string idCorta = transactionId[..Math.Min(8, transactionId.Length)];

            if (estaEnVentana)
            {
                return $"✅ Transacción {idCorta}... APROBADA por [{rolUsuario}] {usuarioId}.\n" +
                       $"• Emisión Interna: {txOriginal.SwiftEmisionInternaTimestamp:yyyy-MM-dd HH:mm:ss}\n" +
                       $"• Transmisión Efectiva SWIFT: {txOriginal.SwiftTransmisionEfectivaTimestamp:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                return $"✅ Transacción {idCorta}... APROBADA por [{rolUsuario}] {usuarioId}.\n" +
                       $"• Emisión Interna: {txOriginal.SwiftEmisionInternaTimestamp:yyyy-MM-dd HH:mm:ss}\n" +
                       $"• Transmisión SWIFT Programada: {txOriginal.SwiftTransmisionEfectivaTimestamp:yyyy-MM-dd HH:mm} (Encolada en Cola 3).";
            }
        }

        public string RechazarTransaccionConListaCliente(string transactionId, string ejecutivoId, MotivoRechazoBancario motivoEnum, Transaction txOriginal)
        {
            if (!AuthManager.PuedeAprobarObservadas())
            {
                return "❌ Acceso denegado por Segregación de Funciones: El perfil Administrador no tiene permitido intervenir en la gestión de transferencias.";
            }

            string motivoRechazo = motivoEnum.ObtenerDescripcion();

            var revision = _bandejaRevisiones.Find(r => r.TransactionId == transactionId);
            if (revision == null) return "❌ Error: Transacción no encontrada.";

            if (revision.EstadoRevision != "Pendiente")
                return $"⚠️ Acción rechazada: La transacción ya fue procesada como '{revision.EstadoRevision}'.";

            if (_idsProcesados.Contains(transactionId))
                return $"❌ Error de atomicidad: El ID {transactionId} ya cuenta con una resolución.";

            DateTime timestampUtc = ConfiguracionOperativa.ObtenerFechaHoraActual();
            revision.EstadoRevision = "Rechazado";
            revision.EvaluadoPorEjecutivoId = ejecutivoId;
            revision.EvaluationTimestamp = timestampUtc;
            revision.MotivoRevision += $" | RECHAZADO ({motivoEnum}) por {ejecutivoId}: {motivoRechazo}";

            txOriginal.Status = "Rejected";

            // Sincronizar actualización en log de observadas
            ArchivoManager.ActualizarEstadoOperacionObservada(transactionId, "Rejected", timestampUtc, motivoRechazo);

            ArchivoManager.RegistrarDecisionEjecutivo(
                transactionId: transactionId,
                sourceAccount: txOriginal.SourceAccountNumber,
                amount: txOriginal.Amount,
                currency: txOriginal.Currency,
                destinationCurrency: txOriginal.DestinationCurrency,
                ejecutivoId: ejecutivoId,
                estadoDecision: "Rechazada",
                detalleMotivo: motivoRechazo,
                timestampDecision: timestampUtc,
                prioridadAsignada: txOriginal.PrioridadAsignada
            );

            _idsProcesados.Add(transactionId);

            var registroCliente = new RegistroRechazoCliente
            {
                TransactionId = transactionId,
                SolicitudOriginal = txOriginal,
                EjecutivoId = ejecutivoId,
                ObservacionBanco = motivoRechazo,
                TimestampRechazo = timestampUtc
            };

            _listaRechazosClientes.Add(registroCliente);

            string timestampStr = timestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
            return $"🛡️ Transacción {transactionId[..Math.Min(8, transactionId.Length)]}... RECHAZADA bajo motivo estandarizado a las {timestampStr}.";
        }
    }
}