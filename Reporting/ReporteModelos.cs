using System;
using System.Collections.Generic;

namespace SistemaBancario.Reporting
{
    public enum OpcionReporteOperacionActual
    {
        SoloIndicadores,
        SoloDetalleTransacciones,
        Ambos
    }

    public class IndicadoresGerencialesDto
    {
        public int TotalSolicitudes { get; set; }
        public int TotalProcesadasAprobadas { get; set; }
        public int TotalRechazadas { get; set; }
        public int TotalExcepcionesSistema { get; set; }
        public decimal MontoTotalProcesadoUSD { get; set; }
        public decimal TasaEfectividadPorcentaje { get; set; }
        public Dictionary<string, decimal> DistribucionMontosPorMoneda { get; set; } = new Dictionary<string, decimal>();
    }

    public class TransaccionOperacionActualDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime HoraIngreso { get; set; }
        public DateTime? SwiftEmisionInternaTimestamp { get; set; }
        public DateTime? SwiftTransmisionEfectivaTimestamp { get; set; }
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string SourceHolderName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal AmountInUSD { get; set; }
        public string DestinationBankName { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public int PrioridadAsignada { get; set; }
        public string StatusActual { get; set; } = string.Empty;
        public string EstadoTransmision { get; set; } = string.Empty;
        public bool HasExceptionFlag { get; set; }
        public bool Acknowledged { get; set; }
    }

    public class ResultadoReporteOperacionActual
    {
        public string TipoReporte { get; set; } = string.Empty;
        public DateTime FechaConsultaUtc { get; set; }
        public List<TransaccionOperacionActualDto> DetalleTransacciones { get; set; } = new List<TransaccionOperacionActualDto>();
        public IndicadoresGerencialesDto? IndicadoresGerenciales { get; set; }
    }
}