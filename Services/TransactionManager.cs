using Sistema_Bancario_de_gestión_de_transacciones_en_C_.Models;
using SistemaBancario.Models;

namespace SistemaBancario.Services
{
    public static class TransactionManager
    {
        // Repositorio de cuentas autorizadas por Empresa
        private static readonly Dictionary<string, List<CuentaBancaria>> CuentasAutorizadasPorEmpresa = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "TechCorp S.A.C.", new List<CuentaBancaria>
                {
                    new CuentaBancaria { SourceHolderName = "TechCorp S.A.C.", NumeroCuenta = "193-000111-0-11", Moneda = "USD" },
                    new CuentaBancaria { SourceHolderName = "TechCorp S.A.C.", NumeroCuenta = "193-000222-0-22", Moneda = "PEN" }
                }
            },
            {
                "Logística Global S.A.", new List<CuentaBancaria>
                {
                    new CuentaBancaria { SourceHolderName = "Logística Global S.A.", NumeroCuenta = "200-999888-1-55", Moneda = "USD" }
                }
            }
        };

        /// <summary>
        /// Valida y crea una transacción asegurando que la cuenta de origen y su moneda 
        /// pertenezcan estrictamente a la empresa asociada al UsuarioCliente.
        /// </summary>
        public static Transaction CrearTransactionSegura(
            UsuarioCliente cliente,
            string cuentaOrigen,
            string monedaOrigen,
            string bancoDestino,
            string swiftDestino,
            string cuentaDestino,
            string monedaDestino,
            string titularDestino,
            decimal monto)
        {
            string empresa = cliente.CompanyName ?? string.Empty;

            // 1. Validar que la empresa del cliente esté registrada en el sistema corporativo
            if (string.IsNullOrWhiteSpace(empresa) || !CuentasAutorizadasPorEmpresa.ContainsKey(empresa))
            {
                throw new UnauthorizedAccessException($"El cliente '{cliente.Nombres} {cliente.Apellidos}' pertenece a la empresa '{empresa}', la cual no tiene un registro de cuentas corporativas válidas.");
            }

            // 2. Obtener las cuentas autorizadas para dicha empresa
            var cuentasDeLaEmpresa = CuentasAutorizadasPorEmpresa[empresa];

            // 3. Validar estrictamente que la cuenta de origen pertenezca a la empresa Y coincida exactamente con su tipo de moneda
            var cuentaValida = cuentasDeLaEmpresa.FirstOrDefault(c =>
                c.NumeroCuenta.Equals(cuentaOrigen, StringComparison.OrdinalIgnoreCase) &&
                c.Moneda.Equals(monedaOrigen, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException(
                    $"Violación de seguridad y cumplimiento: La cuenta de origen '{cuentaOrigen}' con moneda '{monedaOrigen}' " +
                    $"no está autorizada o no pertenece a la empresa '{empresa}' para la cual labora el usuario."
                );

            // 4. Si pasa todas las validaciones de negocio, se instancia la transacción de forma segura
            return new Transaction
            {
                Currency = monedaOrigen,
                DestinationCurrency = monedaDestino,
                SourceAccountNumber = cuentaValida.NumeroCuenta,
                SourceHolderName = empresa, // El titular de origen es obligatoriamente el nombre de la empresa
                DestinationBankName = bancoDestino,
                DestinationSwiftCode = swiftDestino,
                DestinationAccountNumber = cuentaDestino,
                DestinationHolderName = titularDestino,
                Amount = monto,
                Status = "Pending",
                RiskFlag = "Normal"
            };
        }
    }
}