using Sistema_Bancario_de_gestión_de_transacciones_en_C_.Models;
using SistemaBancario.Models;
using System.Text;

namespace SistemaBancario.Security
{
    public static class AccountFileAdapter
    {
        private static readonly string RutaDirectorio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string RutaArchivo = Path.Combine(RutaDirectorio, "cuentas.csv");

        /// <summary>
        /// Carga todas las cuentas bancarias desde el CSV en disco. Si no existe, genera datos por defecto.
        /// </summary>
        public static List<CuentaBancaria> CargarCuentas()
        {
            var listaCuentas = new List<CuentaBancaria>();

            try
            {
                if (!Directory.Exists(RutaDirectorio))
                {
                    Directory.CreateDirectory(RutaDirectorio);
                }

                if (!File.Exists(RutaArchivo))
                {
                    listaCuentas = ObtenerCuentasPorDefecto();
                    GuardarCuentas(listaCuentas);
                    return listaCuentas;
                }

                var lineas = File.ReadAllLines(RutaArchivo, Encoding.UTF8);
                for (int i = 1; i < lineas.Length; i++) // Omitir cabecera
                {
                    var linea = lineas[i];
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var partes = linea.Split(';');
                    if (partes.Length >= 3)
                    {
                        listaCuentas.Add(new CuentaBancaria
                        {
                            SourceHolderName = partes[0].Trim('"'),
                            NumeroCuenta = partes[1].Trim('"'),
                            Moneda = partes[2].Trim('"')
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar cuentas desde disco: {ex.Message}");
                listaCuentas = ObtenerCuentasPorDefecto();
            }

            return listaCuentas;
        }

        /// <summary>
        /// Filtra y devuelve las cuentas permitidas según el usuario activo.
        /// </summary>
        public static List<CuentaBancaria> ObtenerCuentasPermitidasParaUsuarioActual()
        {
            var todasLasCuentas = CargarCuentas();
            var usuario = AuthManager.UsuarioActual;

            if (usuario == null) return new List<CuentaBancaria>();

            // El Administrador tiene acceso total a todas las cuentas
            if (usuario.Role == UserRole.Admin)
            {
                return todasLasCuentas;
            }

            // El Cliente solo visualiza las cuentas vinculadas a su CompanyName / SourceHolderName
            if (usuario is UsuarioCliente cliente)
            {
                return todasLasCuentas
                    .Where(c => c.SourceHolderName.Equals(cliente.CompanyName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Gerentes y Ejecutivos tienen acceso de supervisión general
            if (usuario.Role == UserRole.Manager || usuario.Role == UserRole.Executive)
            {
                return todasLasCuentas;
            }

            return new List<CuentaBancaria>();
        }

        /// <summary>
        /// Guarda el listado completo de cuentas en el archivo CSV.
        /// </summary>
        public static void GuardarCuentas(List<CuentaBancaria> cuentas)
        {
            try
            {
                if (!Directory.Exists(RutaDirectorio))
                {
                    Directory.CreateDirectory(RutaDirectorio);
                }

                var sb = new StringBuilder();
                sb.AppendLine("SourceHolderName;NumeroCuenta;Moneda");

                foreach (var c in cuentas)
                {
                    sb.AppendLine($"\"{c.SourceHolderName}\";\"{c.NumeroCuenta}\";\"{c.Moneda}\"");
                }

                File.WriteAllText(RutaArchivo, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar cuentas en disco: {ex.Message}");
            }
        }

        // =========================================================================
        // NUEVOS MÉTODOS DE REGISTRO SEGÚN EL ROL
        // =========================================================================

        /// <summary>
        /// Permite que el cliente autenticado registre una nueva cuenta propia vinculada a su empresa.
        /// </summary>
        public static bool RegistrarCuentaCliente(string numeroCuenta, string moneda)
        {
            var usuario = AuthManager.UsuarioActual;

            // Casteo directo con pattern matching
            if (usuario is not UsuarioCliente cliente)
            {
                return false;
            }

            var cuentas = CargarCuentas();

            if (cuentas.Exists(c => c.NumeroCuenta.Equals(numeroCuenta, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            cuentas.Add(new CuentaBancaria
            {
                SourceHolderName = cliente.CompanyName,
                NumeroCuenta = numeroCuenta,
                Moneda = moneda
            });

            GuardarCuentas(cuentas);
            return true;
        }

        /// <summary>
        /// Permite que el Administrador registre cuentas para cualquier empresa o titular.
        /// </summary>
        public static bool RegistrarCuentaAdmin(string sourceHolderName, string numeroCuenta, string moneda)
        {
            var usuario = AuthManager.UsuarioActual;
            if (usuario == null || usuario.Role != UserRole.Admin)
            {
                return false;
            }

            var cuentas = CargarCuentas();

            if (cuentas.Exists(c => c.NumeroCuenta.Equals(numeroCuenta, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            cuentas.Add(new CuentaBancaria
            {
                SourceHolderName = sourceHolderName,
                NumeroCuenta = numeroCuenta,
                Moneda = moneda
            });

            GuardarCuentas(cuentas);
            return true;
        }

        /// <summary>
        /// Cuentas iniciales por defecto para la primera ejecución.
        /// </summary>
        private static List<CuentaBancaria> ObtenerCuentasPorDefecto()
        {
            return new List<CuentaBancaria>
            {
                new CuentaBancaria { SourceHolderName = "Empresa Cliente SAC", NumeroCuenta = "ACC-SOL-9012", Moneda = "PEN" },
                new CuentaBancaria { SourceHolderName = "Empresa Cliente SAC", NumeroCuenta = "ACC-USD-9013", Moneda = "USD" },
                new CuentaBancaria { SourceHolderName = "Sucursal Principal SAC", NumeroCuenta = "ACC-SOL-1001", Moneda = "PEN" },
                new CuentaBancaria { SourceHolderName = "Sucursal Principal SAC", NumeroCuenta = "ACC-EUR-1002", Moneda = "EUR" },
                new CuentaBancaria { SourceHolderName = "Departamento de Compliance", NumeroCuenta = "ACC-SOL-5501", Moneda = "PEN" },
                new CuentaBancaria { SourceHolderName = "Sistema Bancario Admin", NumeroCuenta = "ACC-SYS-0001", Moneda = "USD" }
            };
        }
    }
}