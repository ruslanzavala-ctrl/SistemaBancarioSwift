using SistemaBancario.DataStructures;
using SistemaBancario.Models;
using SistemaBancario.Persistence;
using SistemaBancario.Security;

namespace SistemaBancario.Services
{
    /// <summary>
    /// Almacena los parámetros operativos globales configurables por el Administrador desde la GUI.
    /// </summary>
    public static class ConfiguracionOperativa
    {
        public static TimeSpan HorarioCorteSwift { get; set; } = new TimeSpan(14, 0, 0);   // Límite emisión SWIFT (14:00 hrs)
        public static TimeSpan HorarioCorteRevision { get; set; } = new TimeSpan(20, 0, 0); // Límite atención ejecutivo (20:00 hrs)
        public static TimeSpan HorarioInicioJornada { get; set; } = new TimeSpan(8, 0, 0);  // Apertura de operaciones (08:00 hrs)

        // Bandera configurable para zona horaria del sistema
        public static bool UsarHoraUtc { get; set; } = false; // false = Hora Local del Sistema (por defecto), true = UTC

        // =========================================================================
        // ENTORNO GLOBAL DE TRABAJO (PRODUCCIÓN / PRUEBAS)
        // =========================================================================
        /// <summary>
        /// Define si los formularios y servicios del sistema leen/escriben en 'DataLogsPruebas' (true) o en 'DataLogs' (false).
        /// </summary>
        public static bool ModoPruebasActivo { get; set; } = false;

        public static DateTime ObtenerFechaHoraActual()
        {
            return UsarHoraUtc ? DateTime.UtcNow : DateTime.Now;
        }

        public static int HoraCorteSwift
        {
            get => HorarioCorteSwift.Hours;
            set => HorarioCorteSwift = new TimeSpan(value, 0, 0);
        }

        public static int HoraCorteRevisionManual
        {
            get => HorarioCorteRevision.Hours;
            set => HorarioCorteRevision = new TimeSpan(value, 0, 0);
        }
    }

    /// <summary>
    /// Reglas de evaluación de negocio para clasificación de riesgo y derivación a revisión manual.
    /// Puntuación aditiva de prioridad de atención:
    /// - Monto < $5,000 USD: +1 punto
    /// - Monto > $5,000 USD: +3 puntos
    /// - Cliente/Proveedor Corporativo: +5 puntos
    /// - Cliente/Proveedor No Corporativo: +1 punto
    /// Valores posibles de prioridad: 2, 4, 6, 8.
    /// Desempate: Se ordena estrictamente por fecha/hora de llegada (FIFO).
    /// </summary>
    public static class GestorReglasNegocio
    {
        public static bool DebeIrARevisionManual(Transaction tx, List<Transaction> historialOperacionesHoy)
        {
            // Regla 1: Montos equivalentes superiores a $5,000 USD sin antecedente frecuente en el día
            decimal montoEvaluarUSD = tx.AmountInUSD > 0 ? tx.AmountInUSD : tx.Amount;

            if (montoEvaluarUSD > 5000m)
            {
                bool esMismoBeneficiarioFrecuente = historialOperacionesHoy.Any(h =>
                    h.DestinationAccountNumber == tx.DestinationAccountNumber &&
                    (h.Status == "Processed-Completed" || h.Status == "Revised-Completed" || h.Status == "Processed" || h.Status == "Revised"));

                if (!esMismoBeneficiarioFrecuente)
                {
                    tx.RiskFlag = "Monto Elevado sin Historial Frecuente";
                    return true;
                }
            }

            // Regla 2: Moneda extranjera de alto valor fuera del horario preferencial
            if (tx.Currency == "EUR" && tx.Amount > 50000m)
            {
                tx.RiskFlag = "Operación Divisa EUR de Alto Impacto";
                return true;
            }

            tx.RiskFlag = "Normal";
            return false;
        }

        /// <summary>
        /// Calcula la prioridad aditiva según el perfil corporativo y el monto convertido a USD:
        /// - Prioridad 8: Corporativo (+5) Y Monto > $5,000 USD (+3)
        /// - Prioridad 6: Corporativo (+5) Y Monto < $5,000 USD (+1)
        /// - Prioridad 4: No Corporativo (+1) Y Monto > $5,000 USD (+3)
        /// - Prioridad 2: No Corporativo (+1) Y Monto < $5,000 USD (+1)
        /// </summary>
        public static int CalcularPrioridadAtencion(Transaction tx, bool esClienteCorporativo)
        {
            int prioridad = 0;
            decimal montoUSD = tx.AmountInUSD > 0 ? tx.AmountInUSD : tx.Amount;

            // 1. Evaluación por Tipo de Cliente / Proveedor
            if (esClienteCorporativo)
            {
                prioridad += 5; // +5 Puntos para Corporativos
            }
            else
            {
                prioridad += 1; // +1 Punto para No Corporativos
            }

            // 2. Evaluación por Rango de Monto equivalente en USD
            if (montoUSD > 5000m)
            {
                prioridad += 3; // +3 Puntos para Montos > $5,000 USD
            }
            else
            {
                prioridad += 1; // +1 Punto para Montos <= $5,000 USD
            }

            return prioridad;
        }

        public static int CalcularPrioridadAtencion(Transaction tx, UsuarioCliente? cliente)
        {
            bool esCorp = cliente != null && cliente.EsCorporativo;
            return CalcularPrioridadAtencion(tx, esCorp);
        }

        public static int CalcularPrioridadAtencion(Transaction tx)
        {
            bool esCorp = false;
            if (AuthManager.UsuarioActual is UsuarioCliente clienteActual)
            {
                esCorp = clienteActual.EsCorporativo;
            }
            return CalcularPrioridadAtencion(tx, esCorp);
        }
    }

    public class GestorTransacciones
    {
        public ColaDinamicaPrioritaria ColaOperaciones { get; private set; }
        public ColaDinamicaPrioritaria ColaRevisionManual { get; private set; }
        public List<TransaccionRevision> HistoricoRevisiones { get; private set; }

        public GestorTransacciones()
        {
            ColaOperaciones = new ColaDinamicaPrioritaria();
            ColaRevisionManual = new ColaDinamicaPrioritaria();
            HistoricoRevisiones = new List<TransaccionRevision>();
        }

        /// <summary>
        /// Procesar transacción pasando el objeto del perfil del cliente (Recomendado)
        /// </summary>
        public string ProcesarSolicitudTransferencia(Transaction tx, UsuarioCliente cliente, bool esFrecuente)
        {
            bool esCorp = cliente != null && cliente.EsCorporativo;
            return ProcesarSolicitudTransferencia(tx, esCorp, esFrecuente);
        }

        /// <summary>
        /// Sobrecarga compatible con bandera booleana para llamadas legadas
        /// </summary>
        public string ProcesarSolicitudTransferencia(Transaction tx, bool esClienteCorporativo, bool esFrecuente)
        {
            // 1. Garantizar la conversión de la divisa ingresada a su equivalente en USD usando TipoCambio
            if (tx.AmountInUSD <= 0)
            {
                TipoCambio tc = ArchivoManager.ObtenerTipoCambioVigente(tx.ExecutionTimestamp);
                tx.AmountInUSD = tc.ConvertirAMontoUSD(tx.Amount, tx.Currency);
            }

            // 2. Calcular la prioridad aditiva (8, 6, 4, 2) sobre el valor equivalente en USD
            int prioridadCalculada = GestorReglasNegocio.CalcularPrioridadAtencion(tx, esClienteCorporativo);

            return EjecutarProcesamientoInterno(tx, prioridadCalculada, esFrecuente);
        }

        private string EjecutarProcesamientoInterno(Transaction tx, int prioridadCalculada, bool esFrecuente)
        {
            tx.PrioridadAsignada = prioridadCalculada;
            string idCorta = tx.TransactionId.Length >= 6 ? tx.TransactionId.Substring(0, 6) : tx.TransactionId;
            DateTime ahora = ConfiguracionOperativa.ObtenerFechaHoraActual();

            // Evaluar reglas para determinar si requiere revisión manual
            bool requiereRevision = GestorReglasNegocio.DebeIrARevisionManual(tx, new List<Transaction>());

            if (requiereRevision || !esFrecuente)
            {
                tx.Status = "UnderReview";
                tx.ManualReviewTimestamp = ahora;

                var revision = new TransaccionRevision
                {
                    TransactionId = tx.TransactionId,
                    MotivoRevision = string.IsNullOrEmpty(tx.RiskFlag) ? "Operación sujeta a revisión manual" : tx.RiskFlag,
                    EstadoRevision = "Pendiente"
                };
                HistoricoRevisiones.Add(revision);

                // Ingresa a la Cola 2 (Revisión Manual)
                ColaRevisionManual.EncolarConPrioridad(tx, prioridadCalculada);

                return $"[EN REVISIÓN MANUAL] La transacción {idCorta}... fue enviada a la bandeja del Ejecutivo. Estado: UnderReview (Prioridad {prioridadCalculada}).";
            }

            // =========================================================================
            // APROBACIÓN DIRECTA -> EMISIÓN INTERNA SWIFT + EVALUACIÓN DE TRANSMISIÓN
            // =========================================================================
            tx.Status = "Processed";
            string tramaSwift = tx.GenerarSwift(ahora); // Marca SwiftEmisionInternaTimestamp

            // Evaluamos si el instante de procesamiento cae dentro de la Ventana de Atención SWIFT (08:00 - 14:00 L-V)
            bool estaEnVentana = EsVentanaAtencionSwift(ahora);

            if (estaEnVentana)
            {
                tx.EstadoTransmision = "TransmitidoEfectivo";
                tx.SwiftTransmisionEfectivaTimestamp = ahora;
                tx.Status = "Processed-Completed";

                // Persistir salida en log
                ArchivoManager.RegistrarSwiftSalida(tx, tramaSwift, ahora, esEmisionEjecutivo: false, timestampEjecutivo: null);

                return $"[SWIFT TRANSMITIDO] Operación aprobada e investigada en tiempo real.\n" +
                       $"• Prioridad de Atención: {tx.PrioridadAsignada}\n" +
                       $"• Emisión Interna: {tx.SwiftEmisionInternaTimestamp:yyyy-MM-dd HH:mm:ss}\n" +
                       $"• Transmisión Efectiva SWIFT: {tx.SwiftTransmisionEfectivaTimestamp:yyyy-MM-dd HH:mm:ss}\n\n{tramaSwift}";
            }
            else
            {
                tx.EstadoTransmision = "EncoladoParaTransmision";
                tx.SwiftTransmisionEfectivaTimestamp = CalcularFechaEmisionSwift(ahora);

                // Ingresa a la Cola 3 (Emisión SWIFT)
                ColaOperaciones.EncolarConPrioridad(tx, prioridadCalculada);

                // Persistir estado encolado en log
                ArchivoManager.RegistrarSwiftSalida(tx, tramaSwift, ahora, esEmisionEjecutivo: false, timestampEjecutivo: null);

                return $"[SWIFT EMITIDO INTERNAMENTE - ENCOLADO] Operación aprobada e indexada en la Cola 3.\n" +
                       $"• Prioridad de Atención: {tx.PrioridadAsignada}\n" +
                       $"• Emisión Interna: {tx.SwiftEmisionInternaTimestamp:yyyy-MM-dd HH:mm:ss}\n" +
                       $"• Transmisión SWIFT Programada: {tx.SwiftTransmisionEfectivaTimestamp:yyyy-MM-dd HH:mm} (Apertura de Ventana Operativa).\n\n{tramaSwift}";
            }
        }

        /// <summary>
        /// Evalúa si una fecha/hora dada se encuentra dentro de la ventana de atención interbancaria (08:00 - 14:00 hrs de Lunes a Viernes).
        /// </summary>
        public static bool EsVentanaAtencionSwift(DateTime fecha)
        {
            if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday) return false;

            TimeSpan hora = fecha.TimeOfDay;
            return hora >= ConfiguracionOperativa.HorarioInicioJornada && hora < ConfiguracionOperativa.HorarioCorteSwift;
        }

        public static string DeterminarEstadoConSwift(string estadoActual)
        {
            if (estadoActual == "Revised") return "Revised-Completed";
            if (estadoActual == "Processed" || estadoActual == "Approved" || estadoActual == "Pending") return "Processed-Completed";
            return estadoActual;
        }

        public string AprobarTransaccionManual(string transactionId, string ejecutivoId)
        {
            var revision = HistoricoRevisiones.Find(r => r.TransactionId == transactionId && r.EstadoRevision == "Pendiente");
            if (revision != null)
            {
                revision.EstadoRevision = "Aprobado";
                revision.EvaluadoPorEjecutivoId = ejecutivoId;
                revision.EvaluationTimestamp = CalcularFechaDecisionEjecutivo(ConfiguracionOperativa.ObtenerFechaHoraActual(), 0);

                string idCorta = transactionId.Length >= 6 ? transactionId.Substring(0, 6) : transactionId;
                return $"Transacción {idCorta} aprobada por el ejecutivo. Estado actualizado a 'Revised'.";
            }
            return "No se encontró la orden de revisión especificada.";
        }

        public string RechazarTransaccionManual(string transactionId, string ejecutivoId, string motivo)
        {
            var revision = HistoricoRevisiones.Find(r => r.TransactionId == transactionId && r.EstadoRevision == "Pendiente");
            if (revision != null)
            {
                revision.EstadoRevision = "Rechazado";
                revision.EvaluadoPorEjecutivoId = ejecutivoId;
                revision.EvaluationTimestamp = CalcularFechaDecisionEjecutivo(ConfiguracionOperativa.ObtenerFechaHoraActual(), 0);
                revision.MotivoRevision += $" | RECHAZADO: {motivo}";

                string idCorta = transactionId.Length >= 6 ? transactionId.Substring(0, 6) : transactionId;
                return $"Transacción {idCorta} fue RECHAZADA. Estado actualizado a 'Rejected'.";
            }
            return "No se encontró la orden de revisión especificada.";
        }

        public static string ForzarEmisionSwiftPorEjecutivo(Transaction tx, string ejecutivoId)
        {
            DateTime fechaActual = ConfiguracionOperativa.ObtenerFechaHoraActual();

            tx.Status = DeterminarEstadoConSwift(tx.Status);
            tx.ApprovedByUserId = ejecutivoId;

            // Emisión Interna + Transmisión Efectiva Manual por Ejecutivo
            string tramaSwift = tx.GenerarSwift(fechaActual);
            tx.EstadoTransmision = "TransmitidoEfectivo";
            tx.SwiftTransmisionEfectivaTimestamp = fechaActual;

            // Persistir en DataLog con bandera y timestamp de ejecutivo
            ArchivoManager.RegistrarSwiftSalida(
                tx,
                tramaSwift,
                fechaActual,
                esEmisionEjecutivo: true,
                timestampEjecutivo: fechaActual
            );

            string idCorta = tx.TransactionId.Length >= 6 ? tx.TransactionId.Substring(0, 6) : tx.TransactionId;
            return $"[SWIFT FORZADO TRANSMITIDO] Transacción {idCorta} transmitida exitosamente por Ejecutivo [{ejecutivoId}]. Estado: {tx.Status} (Prioridad {tx.PrioridadAsignada}).";
        }

        public static DateTime CalcularFechaEmisionSwift(DateTime fechaSolicitud)
        {
            TimeSpan horaSolicitud = fechaSolicitud.TimeOfDay;

            if (horaSolicitud < ConfiguracionOperativa.HorarioInicioJornada)
            {
                return fechaSolicitud.Date.Add(ConfiguracionOperativa.HorarioInicioJornada);
            }

            if (horaSolicitud >= ConfiguracionOperativa.HorarioCorteSwift)
            {
                DateTime siguienteDia = fechaSolicitud.Date.AddDays(1);
                while (siguienteDia.DayOfWeek == DayOfWeek.Saturday || siguienteDia.DayOfWeek == DayOfWeek.Sunday)
                {
                    siguienteDia = siguienteDia.AddDays(1);
                }
                return siguienteDia.Add(ConfiguracionOperativa.HorarioInicioJornada);
            }

            return fechaSolicitud.Date.Add(ConfiguracionOperativa.HorarioInicioJornada);
        }

        public static DateTime CalcularFechaDecisionEjecutivo(DateTime fechaSolicitud, int tiempoRevisionMin)
        {
            DateTime fechaEstimada = fechaSolicitud.AddMinutes(tiempoRevisionMin);

            if (fechaEstimada.TimeOfDay >= ConfiguracionOperativa.HorarioCorteRevision)
            {
                return fechaSolicitud.Date.AddDays(1).Add(ConfiguracionOperativa.HorarioInicioJornada);
            }
            return fechaEstimada;
        }
    }
}