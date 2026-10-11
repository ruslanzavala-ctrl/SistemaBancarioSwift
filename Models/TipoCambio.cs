namespace SistemaBancario.Models
{
    public class TipoCambio
    {
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public string ManagerUsername { get; set; } = string.Empty;

        // Tasas base ingresadas por el gerente (con respecto a Soles - PEN)
        public decimal TipoCambioUsdPen { get; set; } = 3.7500m; // 1 USD = 3.7500 PEN
        public decimal TipoCambioEurPen { get; set; } = 4.0500m; // 1 EUR = 4.0500 PEN

        // Cálculo automático de tasa triangulada USD a EUR y EUR a USD
        public decimal TipoCambioUsdEur => TipoCambioEurPen > 0 ? TipoCambioUsdPen / TipoCambioEurPen : 0.9259m;
        public decimal TipoCambioEurUsd => TipoCambioUsdPen > 0 ? TipoCambioEurPen / TipoCambioUsdPen : 1.0800m;

        /// <summary>
        /// Convierte cualquier monto (USD, EUR, PEN) a su equivalente exacto en USD.
        /// </summary>
        public decimal ConvertirAMontoUSD(decimal monto, string moneda)
        {
            if (string.IsNullOrWhiteSpace(moneda)) return monto;

            moneda = moneda.Trim().ToUpper();

            switch (moneda)
            {
                case "USD":
                    return monto;

                case "EUR":
                    // EUR -> USD usando la tasa triangulada (EUR/PEN / USD/PEN)
                    return TipoCambioUsdPen > 0 ? Math.Round(monto * (TipoCambioEurPen / TipoCambioUsdPen), 2) : monto;

                case "PEN":
                case "SOL":
                case "SOLES":
                    // PEN -> USD
                    return TipoCambioUsdPen > 0 ? Math.Round(monto / TipoCambioUsdPen, 2) : monto;

                default:
                    return monto;
            }
        }
    }
}