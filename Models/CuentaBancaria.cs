namespace Sistema_Bancario_de_gestión_de_transacciones_en_C_.Models
{
    public class CuentaBancaria
    {
        // Vinculación con el titular / empresa para el autocompletado y filtros de rol
        public required string SourceHolderName { get; set; }
        public required string NumeroCuenta { get; set; }
        public required string Moneda { get; set; } // "PEN", "USD", etc.
    }
}