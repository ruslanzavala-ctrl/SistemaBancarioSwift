using SistemaBancario.Models;
using SistemaBancario.Persistence;
using SistemaBancario.Security;
using SistemaBancario.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaBancario.Reporting
{
    public static class ReporteManager
    {
        public static int ResolverPrioridadEfectiva(Transaction tx, List<User> usuarios)
        {
            if (tx.PrioridadAsignada > 1 && tx.PrioridadAsignada <= 8 && tx.PrioridadAsignada % 2 == 0)
            {
                return tx.PrioridadAsignada;
            }

            bool esCorporativo = false;
            string titularLower = (tx.SourceHolderName ?? string.Empty).Trim().ToLower();

            var usuarioEmpresa = usuarios.FirstOrDefault(u =>
                u.SourceHolderName != null &&
                u.SourceHolderName.Trim().ToLower().Equals(titularLower));

            if (usuarioEmpresa is UsuarioCliente cliente)
            {
                esCorporativo = cliente.EsCorporativo;
            }
            else if (titularLower.Contains("corp") || titularLower.Contains("sac") || titularLower.Contains("s.a.") || titularLower.Contains("inc"))
            {
                esCorporativo = true;
            }

            return GestorReglasNegocio.CalcularPrioridadAtencion(tx, esCorporativo);
        }

        public static ResultadoReporteOperacionActual ObtenerReporteOperacionActual(
            DateTime fechaConsulta,
            OpcionReporteOperacionActual opcion = OpcionReporteOperacionActual.Ambos,
            bool? esPrueba = null)
        {
            var resultado = new ResultadoReporteOperacionActual
            {
                TipoReporte = "Reporte de Operación Actual (Tiempo Real)",
                FechaConsultaUtc = fechaConsulta
            };

            var usuariosActuales = AuthManager.ObtenerTodosLosUsuarios();
            var solicitudesHoy = ArchivoManager.CargarSolicitudesDiarias(fechaConsulta, esPrueba);
            var observadasHoy = ArchivoManager.CargarOperacionesObservadas(fechaConsulta, esPrueba);

            var dictObservadas = observadasHoy.ToDictionary(o => o.Transaction.TransactionId, StringComparer.OrdinalIgnoreCase);

            var listaDetalle = new List<TransaccionOperacionActualDto>();

            foreach (var tx in solicitudesHoy)
            {
                int prioridadEfectiva = ResolverPrioridadEfectiva(tx, usuariosActuales);
                tx.PrioridadAsignada = prioridadEfectiva;

                string estadoProceso = tx.Status;
                string estadoTransmision = string.IsNullOrEmpty(tx.EstadoTransmision) ? "Pendiente" : tx.EstadoTransmision;
                bool tieneExcepcion = false;

                if (dictObservadas.TryGetValue(tx.TransactionId, out var obs))
                {
                    estadoProceso = obs.StatusTransaccion;
                    if (obs.EstadoDecision == "Rechazado" || obs.StatusTransaccion == "Rejected")
                    {
                        estadoProceso = "Rejected";
                        tieneExcepcion = true;
                    }
                }

                listaDetalle.Add(new TransaccionOperacionActualDto
                {
                    TransactionId = tx.TransactionId,
                    HoraIngreso = tx.ExecutionTimestamp,
                    SwiftEmisionInternaTimestamp = tx.SwiftEmisionInternaTimestamp,
                    SwiftTransmisionEfectivaTimestamp = tx.SwiftTransmisionEfectivaTimestamp,
                    SourceAccountNumber = tx.SourceAccountNumber,
                    SourceHolderName = tx.SourceHolderName,
                    Amount = tx.Amount,
                    Currency = tx.Currency,
                    AmountInUSD = tx.AmountInUSD > 0 ? tx.AmountInUSD : tx.Amount,
                    DestinationBankName = tx.DestinationBankName,
                    DestinationAccountNumber = tx.DestinationAccountNumber,
                    PrioridadAsignada = prioridadEfectiva,
                    StatusActual = estadoProceso,
                    EstadoTransmision = estadoTransmision,
                    HasExceptionFlag = tieneExcepcion,
                    Acknowledged = false
                });
            }

            resultado.DetalleTransacciones = listaDetalle
                .OrderByDescending(t => t.PrioridadAsignada)
                .ThenBy(t => t.HoraIngreso)
                .ToList();

            if (opcion == OpcionReporteOperacionActual.SoloIndicadores || opcion == OpcionReporteOperacionActual.Ambos)
            {
                resultado.IndicadoresGerenciales = CalcularIndicadores(resultado.DetalleTransacciones);
            }

            return resultado;
        }

        public static List<TransaccionOperacionActualDto> ObtenerColaSwiftPendienteTiempoReal(
            DateTime fechaConsulta,
            bool? esPrueba = null)
        {
            var r = ObtenerReporteOperacionActual(fechaConsulta, OpcionReporteOperacionActual.SoloDetalleTransacciones, esPrueba);

            return (r.DetalleTransacciones ?? new List<TransaccionOperacionActualDto>())
                .Where(t => t.EstadoTransmision == "EncoladoParaTransmision" && t.StatusActual != "Rejected")
                .OrderByDescending(t => t.PrioridadAsignada)
                .ThenBy(t => t.HoraIngreso)
                .ToList();
        }

        private static IndicadoresGerencialesDto CalcularIndicadores(List<TransaccionOperacionActualDto> lista)
        {
            int total = lista.Count;
            int procesadas = lista.Count(t => t.StatusActual == "Processed" || t.StatusActual == "Processed-Completed" || t.StatusActual == "Revised-Completed");
            int rechazadas = lista.Count(t => t.StatusActual == "Rejected");
            int excepciones = lista.Count(t => t.HasExceptionFlag);

            decimal montoTotalUSD = lista.Sum(t => t.AmountInUSD);
            decimal tasaEfectividad = total > 0 ? ((decimal)procesadas / total) * 100m : 0m;

            var distMonedas = lista.GroupBy(t => t.Currency)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.AmountInUSD));

            return new IndicadoresGerencialesDto
            {
                TotalSolicitudes = total,
                TotalProcesadasAprobadas = procesadas,
                TotalRechazadas = rechazadas,
                TotalExcepcionesSistema = excepciones,
                MontoTotalProcesadoUSD = montoTotalUSD,
                TasaEfectividadPorcentaje = tasaEfectividad,
                DistribucionMontosPorMoneda = distMonedas
            };
        }
    }
}