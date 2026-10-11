namespace SistemaBancario.Models
{
    public class Transaction
    {
        // Identificadores y montos básicos
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();
        public decimal Amount { get; set; }

        // Monto equivalente convertido a USD para cálculos, reglas de prioridad y KPIs
        public decimal AmountInUSD { get; set; }

        // La moneda se asigna desde la cuenta de origen del cliente
        public required string Currency { get; set; }

        // Datos de Origen obligatorios
        public required string SourceAccountNumber { get; set; }
        public required string SourceHolderName { get; set; }

        // Datos de Destino obligatorios
        public required string DestinationBankName { get; set; }
        public required string DestinationSwiftCode { get; set; }
        public required string DestinationAccountNumber { get; set; }
        public required string DestinationCurrency { get; set; }
        public required string DestinationHolderName { get; set; }

        // Trazabilidad de Auditoría (Línea de tiempo del ciclo de vida)
        public DateTime ExecutionTimestamp { get; set; } = DateTime.UtcNow;        // Solicitud inicial del cliente
        public DateTime? PriorityEvaluationTimestamp { get; set; }              // Cálculo de prioridad aditiva
        public DateTime? FrequencyEvaluationTimestamp { get; set; }            // Evaluación de frecuencia del beneficiario
        public DateTime? ManualReviewTimestamp { get; set; }                   // Derivación a la Cola 2 (Revisión Manual)
        public DateTime? ApprovalTimestamp { get; set; }                       // Aprobación manual por Ejecutivo/Manager

        // SWIFT: Separación Conceptual de Emisión Interna vs Transmisión Efectiva
        public DateTime? SwiftEmisionInternaTimestamp { get; set; }            // Generación física del mensaje MT103 en el sistema
        public DateTime? SwiftTransmisionEfectivaTimestamp { get; set; }       // Salida efectiva de la Cola 3 hacia la red SWIFT
        public string EstadoTransmision { get; set; } = "Pendiente";            // GeneradoInterno | EncoladoParaTransmision | TransmitidoEfectivo

        // Propiedad legada para compatibilidad
        public DateTime? SwiftGenerationTimestamp
        {
            get => SwiftEmisionInternaTimestamp;
            set => SwiftEmisionInternaTimestamp = value;
        }

        // Estados y Banderas de Responsabilidad Operativa
        public string Status { get; set; } = "Pending";
        public string? ExecutedByUserId { get; set; }
        public string? ApprovedByUserId { get; set; }
        public string RiskFlag { get; set; } = "Normal";
        public int PrioridadAsignada { get; set; } = 1;

        /// <summary>
        /// Genera la trama de comunicación interbancaria SWIFT MT103 y marca el hito de Emisión Interna.
        /// </summary>
        public string GenerarSwift(DateTime? timestampEmision = null)
        {
            DateTime fechaEmision = timestampEmision ?? DateTime.UtcNow;
            SwiftEmisionInternaTimestamp = fechaEmision;

            if (EstadoTransmision == "Pendiente")
            {
                EstadoTransmision = "GeneradoInterno";
            }

            return $"{{1:F01{DestinationSwiftCode}}}{{2:I103{DestinationSwiftCode}X}}{{4:\n" +
                   $":20:TRN-{TransactionId[..Math.Min(8, TransactionId.Length)].ToUpperInvariant()}\n" +
                   $":32A:{fechaEmision:yyMMdd}{Currency}{Amount:N2}\n" +
                   $":50K:{SourceAccountNumber} / {SourceHolderName}\n" +
                   $":59:{DestinationAccountNumber} / {DestinationHolderName}\n" +
                   $"-}}";
        }
    }
}