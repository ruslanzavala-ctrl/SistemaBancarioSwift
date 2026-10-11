namespace SistemaBancario.Models
{
    public class RegistroRechazoCliente
    {
        public required string TransactionId { get; set; }
        public required Transaction SolicitudOriginal { get; set; }
        public required string EjecutivoId { get; set; }
        public required string ObservacionBanco { get; set; }
        public required DateTime TimestampRechazo { get; set; }
    }
}