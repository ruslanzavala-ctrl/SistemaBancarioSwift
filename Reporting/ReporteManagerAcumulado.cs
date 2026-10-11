using SistemaBancario.Models;
using SistemaBancario.Persistence;
using SistemaBancario.Security;
using SistemaBancario.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SistemaBancario.Reporting
{
    public static class ReporteManagerAcumulado
    {
        public static ResultadoReporteOperacionActual ObtenerReporteRangoFechas(
            DateTime fechaInicio,
            DateTime fechaFin,
            OpcionReporteOperacionActual opcion = OpcionReporteOperacionActual.Ambos,
            bool? esPrueba = null)
        {
            var resultado = new ResultadoReporteOperacionActual
            {
                TipoReporte = $"Consolidado Histórico de SWIFTs Emitidos y Operaciones ({fechaInicio:yyyy-MM-dd} al {fechaFin:yyyy-MM-dd})",
                FechaConsultaUtc = ConfiguracionOperativa.ObtenerFechaHoraActual()
            };

            var usuarios = AuthManager.ObtenerTodosLosUsuarios();
            var listaAcumulada = new List<TransaccionOperacionActualDto>();

            for (DateTime fecha = fechaInicio.Date; fecha <= fechaFin.Date; fecha = fecha.AddDays(1))
            {
                // 1. Cargar las solicitudes del día
                var solicitudesDia = ArchivoManager.CargarSolicitudesDiarias(fecha, esPrueba);
                var observadasDia = ArchivoManager.CargarOperacionesObservadas(fecha, esPrueba);
                var dictObs = observadasDia.ToDictionary(o => o.Transaction.TransactionId, StringComparer.OrdinalIgnoreCase);

                foreach (var tx in solicitudesDia)
                {
                    int prioridad = ReporteManager.ResolverPrioridadEfectiva(tx, usuarios);
                    string estadoProceso = tx.Status;
                    string estadoTransmision = string.IsNullOrEmpty(tx.EstadoTransmision) ? "Pendiente" : tx.EstadoTransmision;
                    bool tieneExcepcion = false;

                    if (dictObs.TryGetValue(tx.TransactionId, out var obs))
                    {
                        estadoProceso = obs.StatusTransaccion;
                        if (obs.EstadoDecision == "Rechazado" || obs.StatusTransaccion == "Rejected")
                        {
                            estadoProceso = "Rejected";
                            tieneExcepcion = true;
                        }
                    }

                    // En el histórico acumulado incluimos las operaciones procesadas, completadas, transmitidas o rechazadas
                    listaAcumulada.Add(new TransaccionOperacionActualDto
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
                        PrioridadAsignada = prioridad,
                        StatusActual = estadoProceso,
                        EstadoTransmision = estadoTransmision,
                        HasExceptionFlag = tieneExcepcion,
                        Acknowledged = false
                    });
                }
            }

            // Ordenar por Prioridad (8 -> 6 -> 4 -> 2) y FIFO por Fecha/Hora de Ingreso
            resultado.DetalleTransacciones = listaAcumulada
                .OrderByDescending(t => t.PrioridadAsignada)
                .ThenBy(t => t.HoraIngreso)
                .ToList();

            // Cálculo de Indicadores KPI sobre el total acumulado de SWIFTs y operaciones
            if (opcion == OpcionReporteOperacionActual.SoloIndicadores || opcion == OpcionReporteOperacionActual.Ambos)
            {
                int total = resultado.DetalleTransacciones.Count;
                int procesadasSWIFT = resultado.DetalleTransacciones.Count(t =>
                    t.EstadoTransmision == "TransmitidoEfectivo" ||
                    t.StatusActual == "Processed-Completed" ||
                    t.StatusActual == "Revised-Completed" ||
                    t.StatusActual == "Processed");

                int rechazadas = resultado.DetalleTransacciones.Count(t => t.StatusActual == "Rejected");
                int excepciones = resultado.DetalleTransacciones.Count(t => t.HasExceptionFlag);

                decimal montoUSD = resultado.DetalleTransacciones
                    .Where(t => t.StatusActual != "Rejected")
                    .Sum(t => t.AmountInUSD);

                decimal efectividad = total > 0 ? ((decimal)procesadasSWIFT / total) * 100m : 0m;

                var distMonedas = resultado.DetalleTransacciones
                    .Where(t => t.StatusActual != "Rejected")
                    .GroupBy(t => t.Currency)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.AmountInUSD));

                resultado.IndicadoresGerenciales = new IndicadoresGerencialesDto
                {
                    TotalSolicitudes = total,
                    TotalProcesadasAprobadas = procesadasSWIFT,
                    TotalRechazadas = rechazadas,
                    TotalExcepcionesSistema = excepciones,
                    MontoTotalProcesadoUSD = montoUSD,
                    TasaEfectividadPorcentaje = efectividad,
                    DistribucionMontosPorMoneda = distMonedas
                };
            }

            return resultado;
        }
    }
}