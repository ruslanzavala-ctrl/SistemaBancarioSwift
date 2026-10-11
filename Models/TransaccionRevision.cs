namespace SistemaBancario.Models
{
    public class TransaccionRevision
    {
        public string RevisionId { get; set; } = Guid.NewGuid().ToString();

        // Campos obligatorios por diseño de negocio
        public required string TransactionId { get; set; }
        public required string MotivoRevision { get; set; }

        public string EstadoRevision { get; set; } = "Pendiente"; // Pendiente, Aprobado, Rechazado
        public string? EvaluadoPorEjecutivoId { get; set; }

        // ==========================================
        // CUMPLIMIENTO Y AUDITORÍA DE REVISIÓN
        // ==========================================
        public DateTime DerivationTimestamp { get; set; } = DateTime.UtcNow;     // Ingreso a revisión manual
        public DateTime? EvaluationTimestamp { get; set; }                    // Acción del ejecutivo (aprobación/rechazo)
    }
}