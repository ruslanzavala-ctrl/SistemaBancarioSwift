using SistemaBancario.Models;
using SistemaBancario.Services;
using SistemaBancario.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SistemaBancario.Models
{
    public class OperacionObservadaDto
    {
        public required Transaction Transaction { get; set; }
        public string MotivoObservacion { get; set; } = string.Empty;
        public DateTime TimestampObservada { get; set; }
        public string EstadoDecision { get; set; } = "Pendiente"; // Pendiente | Aprobado | Rechazado
        public string StatusTransaccion { get; set; } = "UnderReview"; // UnderReview | Processed | Rejected
    }
}

namespace SistemaBancario.Persistence
{
    public static class ArchivoManager
    {
        private static readonly string CarpetaDatos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataLogs");
        private static readonly string CarpetaPruebas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataLogsPruebas");
        private static readonly string RutaArchivoCuentas = Path.Combine(CarpetaDatos, "cuentas_clientes.csv");
        private static readonly string RutaArchivoTipoCambio = Path.Combine(CarpetaDatos, "TipoDeCambio.csv");

        private static readonly object ObjetoBloqueo = new object();

        static ArchivoManager()
        {
            if (!Directory.Exists(CarpetaDatos)) Directory.CreateDirectory(CarpetaDatos);
            if (!Directory.Exists(CarpetaPruebas)) Directory.CreateDirectory(CarpetaPruebas);
        }

        private static string ObtenerCarpetaDestino(bool? esPrueba = null)
        {
            bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
            string carpeta = esPruebaEfectivo ? CarpetaPruebas : CarpetaDatos;
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        // =========================================================================
        // PERSISTENCIA DE TIPOS DE CAMBIO (TipoDeCambio.csv)
        // =========================================================================

        public static void RegistrarHistoricoTipoCambio(string managerUsername, decimal usdPen, decimal eurPen, DateTime? fechaRegistroParam = null)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    DateTime fechaRegistro = fechaRegistroParam ?? ConfiguracionOperativa.ObtenerFechaHoraActual();
                    bool archivoNuevo = !File.Exists(RutaArchivoTipoCambio);

                    using (var sw = new StreamWriter(RutaArchivoTipoCambio, true, Encoding.UTF8))
                    {
                        if (archivoNuevo)
                        {
                            sw.WriteLine("FechaRegistro;ManagerUsername;TipoCambioUsdPen;TipoCambioEurPen");
                        }

                        string fechaFormatted = fechaRegistro.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        string usdPenStr = usdPen.ToString(CultureInfo.InvariantCulture);
                        string eurPenStr = eurPen.ToString(CultureInfo.InvariantCulture);

                        sw.WriteLine($"{fechaFormatted};{managerUsername};{usdPenStr};{eurPenStr}");
                    }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al guardar tipo de cambio: {ex.Message}");
                    throw new InvalidOperationException("No se pudo registrar la tasa de cambio en el datalog.", ex);
                }
            }
        }

        public static TipoCambio ObtenerTipoCambioVigente(DateTime? fechaConsultaParam = null)
        {
            lock (ObjetoBloqueo)
            {
                var tc = new TipoCambio();
                DateTime fechaLimite = fechaConsultaParam ?? ConfiguracionOperativa.ObtenerFechaHoraActual();

                try
                {
                    if (!File.Exists(RutaArchivoTipoCambio)) return tc;

                    var lineas = File.ReadAllLines(RutaArchivoTipoCambio, Encoding.UTF8);
                    for (int i = lineas.Length - 1; i >= 1; i--)
                    {
                        if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                        var partes = lineas[i].Split(';');
                        if (partes.Length >= 4)
                        {
                            if (DateTime.TryParse(partes[0].Trim(), out DateTime fechaReg) && fechaReg <= fechaLimite)
                            {
                                if (decimal.TryParse(partes[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal usdPen) &&
                                    decimal.TryParse(partes[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal eurPen))
                                {
                                    tc.FechaRegistro = fechaReg;
                                    tc.ManagerUsername = partes[1].Trim();
                                    tc.TipoCambioUsdPen = usdPen;
                                    tc.TipoCambioEurPen = eurPen;
                                    return tc;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al leer histórico de tipo de cambio: {ex.Message}");
                }

                return tc;
            }
        }

        // =========================================================================
        // PERSISTENCIA DE CUENTAS POR CLIENTE (cuentas_clientes.csv)
        // =========================================================================

        public static List<CuentaCliente> CargarCuentasCliente(string username)
        {
            lock (ObjetoBloqueo)
            {
                var lista = new List<CuentaCliente>();

                try
                {
                    if (!File.Exists(RutaArchivoCuentas))
                    {
                        using (var sw = new StreamWriter(RutaArchivoCuentas, false, Encoding.UTF8))
                        {
                            sw.WriteLine("Username;NumeroCuenta;Moneda;Estado");
                            sw.WriteLine($"{username};ACC-CLI-001;USD;Activa");
                            sw.WriteLine($"{username};ACC-CLI-EUR-002;EUR;Activa");
                            sw.WriteLine($"{username};ACC-CLI-PEN-003;S/.;Activa");
                        }
                    }

                    var lineas = File.ReadAllLines(RutaArchivoCuentas, Encoding.UTF8);
                    for (int i = 1; i < lineas.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                        var partes = lineas[i].Split(';');
                        if (partes.Length >= 4 && partes[0].Equals(username, StringComparison.OrdinalIgnoreCase))
                        {
                            lista.Add(new CuentaCliente
                            {
                                NumeroCuenta = partes[1].Trim(),
                                Moneda = partes[2].Trim(),
                                Estado = partes[3].Trim()
                            });
                        }
                    }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al cargar cuentas del cliente: {ex.Message}");
                }

                return lista;
            }
        }

        /// <summary>
        /// Carga la totalidad de cuentas y usuarios registrados en cuentas_clientes.csv
        /// para alimentar la regla del 50% de la ingesta masiva.
        /// </summary>
        public static List<(string Username, CuentaCliente Cuenta)> CargarTodasLasCuentasCliente()
        {
            lock (ObjetoBloqueo)
            {
                var lista = new List<(string Username, CuentaCliente Cuenta)>();

                try
                {
                    if (!File.Exists(RutaArchivoCuentas))
                    {
                        CargarCuentasCliente("cliente_demo");
                    }

                    if (File.Exists(RutaArchivoCuentas))
                    {
                        var lineas = File.ReadAllLines(RutaArchivoCuentas, Encoding.UTF8);
                        for (int i = 1; i < lineas.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                            var partes = lineas[i].Split(';');
                            if (partes.Length >= 4)
                            {
                                string user = partes[0].Trim();
                                var cuenta = new CuentaCliente
                                {
                                    NumeroCuenta = partes[1].Trim(),
                                    Moneda = partes[2].Trim(),
                                    Estado = partes[3].Trim()
                                };
                                lista.Add((user, cuenta));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al cargar todas las cuentas de clientes: {ex.Message}");
                }

                return lista;
            }
        }

        public static void GuardarCuentasCliente(string username, List<CuentaCliente> cuentas)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    var todasLasLineas = File.Exists(RutaArchivoCuentas)
                        ? File.ReadAllLines(RutaArchivoCuentas, Encoding.UTF8).ToList()
                        : new List<string> { "Username;NumeroCuenta;Moneda;Estado" };

                    todasLasLineas.RemoveAll(l => l.StartsWith($"{username};", StringComparison.OrdinalIgnoreCase));

                    foreach (var c in cuentas)
                    {
                        todasLasLineas.Add($"{username};{c.NumeroCuenta};{c.Moneda};{c.Estado}");
                    }

                    File.WriteAllLines(RutaArchivoCuentas, todasLasLineas, Encoding.UTF8);
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al guardar cuentas del cliente: {ex.Message}");
                    throw new InvalidOperationException("No se pudieron guardar los cambios en las cuentas bancarias.", ex);
                }
            }
        }

        // =========================================================================
        // LOGS Y PERSISTENCIA DE TRANSACCIONES Y OPERACIONES CON PRIORIDAD
        // =========================================================================

        public static void RegistrarTransaccionSolicitada(Transaction tx, DateTime timestampSolicitud, bool? esPrueba = null)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
                    string carpetaTarget = ObtenerCarpetaDestino(esPruebaEfectivo);
                    string fechaStr = timestampSolicitud.ToString("yyyy-MM-dd");
                    string nombreArchivo = esPruebaEfectivo ? $"datalog_prueba_solicitadas_{fechaStr}.csv" : $"log_transacciones_solicitadas_{fechaStr}.csv";
                    string rutaCompleta = Path.Combine(carpetaTarget, nombreArchivo);

                    bool archivoNuevo = !File.Exists(rutaCompleta);

                    using (var sw = new StreamWriter(rutaCompleta, true, Encoding.UTF8))
                    {
                        if (archivoNuevo)
                        {
                            sw.WriteLine("TransactionId;Amount;Currency;AmountInUSD;SourceAccountNumber;SourceHolderName;DestinationBankName;DestinationSwiftCode;DestinationAccountNumber;DestinationCurrency;DestinationHolderName;ExecutionTimestamp_[yyyy-MM-dd_HH:mm:ss.fff];PriorityEvaluationTimestamp;FrequencyEvaluationTimestamp;ManualReviewTimestamp;ApprovalTimestamp;SwiftEmisionInternaTimestamp;SwiftTransmisionEfectivaTimestamp;EstadoTransmision;Status;ExecutedByUserId;ApprovedByUserId;RiskFlag;PrioridadAsignada;TimestampUtc_[yyyy-MM-dd_HH:mm:ss.fffffff]");
                        }

                        string timestampFormatted = timestampSolicitud.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                        string amountUsdStr = tx.AmountInUSD.ToString(CultureInfo.InvariantCulture);

                        string sourceHolderName = tx.SourceHolderName ?? string.Empty;
                        string destinationHolderName = tx.DestinationHolderName ?? string.Empty;

                        string linea = $"{tx.TransactionId};" +
                                       $"{tx.Amount};" +
                                       $"{tx.Currency};" +
                                       $"{amountUsdStr};" +
                                       $"{tx.SourceAccountNumber};" +
                                       $"\"{sourceHolderName}\";" +
                                       $"{tx.DestinationBankName};" +
                                       $"{tx.DestinationSwiftCode};" +
                                       $"{tx.DestinationAccountNumber};" +
                                       $"{tx.DestinationCurrency};" +
                                       $"\"{destinationHolderName}\";" +
                                       $"{tx.ExecutionTimestamp:yyyy-MM-dd HH:mm:ss.fff};" +
                                       $"{(tx.PriorityEvaluationTimestamp.HasValue ? tx.PriorityEvaluationTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty)};" +
                                       $"{(tx.FrequencyEvaluationTimestamp.HasValue ? tx.FrequencyEvaluationTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty)};" +
                                       $"{(tx.ManualReviewTimestamp.HasValue ? tx.ManualReviewTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty)};" +
                                       $"{(tx.ApprovalTimestamp.HasValue ? tx.ApprovalTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty)};" +
                                       $"{(tx.SwiftEmisionInternaTimestamp.HasValue ? tx.SwiftEmisionInternaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty)};" +
                                       $"{(tx.SwiftTransmisionEfectivaTimestamp.HasValue ? tx.SwiftTransmisionEfectivaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty)};" +
                                       $"{tx.EstadoTransmision};" +
                                       $"{tx.Status};" +
                                       $"{tx.ExecutedByUserId ?? string.Empty};" +
                                       $"{tx.ApprovedByUserId ?? string.Empty};" +
                                       $"{tx.RiskFlag};" +
                                       $"{tx.PrioridadAsignada};" +
                                       $"{timestampFormatted}";

                        sw.WriteLine(linea);
                    }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error de E/S al registrar transacción solicitada: {ex.Message}");
                    DateTime fechaActual = ConfiguracionOperativa.ObtenerFechaHoraActual();
                    RegistrarExcepcionSistema("ArchivoManager", $"Error E/S TransaccionSolicitada: {ex.Message}", tx.TransactionId, fechaActual, esPrueba, tx.PrioridadAsignada);
                    throw new InvalidOperationException("No se pudo persistir la solicitud de transferencia.", ex);
                }
            }
        }

        public static void RegistrarOperacionObservada(Transaction tx, string motivoObservacion, DateTime timestampObservacion, bool? esPrueba = null)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
                    string carpetaTarget = ObtenerCarpetaDestino(esPruebaEfectivo);
                    string fechaStr = timestampObservacion.ToString("yyyy-MM-dd");
                    string nombreArchivo = esPruebaEfectivo ? $"datalog_prueba_observadas_{fechaStr}.csv" : $"log_operaciones_observadas_{fechaStr}.csv";
                    string rutaCompleta = Path.Combine(carpetaTarget, nombreArchivo);

                    bool archivoNuevo = !File.Exists(rutaCompleta);

                    using (var sw = new StreamWriter(rutaCompleta, true, Encoding.UTF8))
                    {
                        if (archivoNuevo)
                        {
                            sw.WriteLine("TransactionId;SourceAccountNumber;Amount;Currency;AmountInUSD;DestinationCurrency;MotivoObservacion;PrioridadAsignada;TimestampUtc_[yyyy-MM-dd_HH:mm:ss.fffffff];EstadoDecision;StatusTransaccion");
                        }

                        string timestampFormatted = timestampObservacion.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                        string amountUsdStr = tx.AmountInUSD.ToString(CultureInfo.InvariantCulture);
                        string estadoDecision = "Pendiente";
                        string statusTx = string.IsNullOrEmpty(tx.Status) ? "UnderReview" : tx.Status;

                        string linea = $"{tx.TransactionId};{tx.SourceAccountNumber};{tx.Amount};{tx.Currency};{amountUsdStr};{tx.DestinationCurrency};\"{motivoObservacion}\";{tx.PrioridadAsignada};{timestampFormatted};{estadoDecision};{statusTx}";

                        sw.WriteLine(linea);
                    }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error de E/S al registrar operación observada: {ex.Message}");
                    DateTime fechaActual = ConfiguracionOperativa.ObtenerFechaHoraActual();
                    RegistrarExcepcionSistema("ArchivoManager", $"Error E/S OperacionObservada: {ex.Message}", tx.TransactionId, fechaActual, esPrueba, tx.PrioridadAsignada);
                    throw new InvalidOperationException("No se pudo persistir el registro de operación observada.", ex);
                }
            }
        }

        // =========================================================================
        // MÉTODOS PARA CONSULTA Y LECTURA DE OBSERVADAS (CON PRIORIDAD CORREGIDA)
        // =========================================================================

        public static List<OperacionObservadaDto> CargarOperacionesObservadas(DateTime fecha, bool? esPrueba = null)
        {
            lock (ObjetoBloqueo)
            {
                var lista = new List<OperacionObservadaDto>();
                try
                {
                    bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
                    string carpetaTarget = ObtenerCarpetaDestino(esPruebaEfectivo);
                    string fechaStr = fecha.ToString("yyyy-MM-dd");
                    string nombreArchivo = esPruebaEfectivo ? $"datalog_prueba_observadas_{fechaStr}.csv" : $"log_operaciones_observadas_{fechaStr}.csv";
                    string rutaCompleta = Path.Combine(carpetaTarget, nombreArchivo);

                    if (!File.Exists(rutaCompleta)) return lista;

                    string[] lineas = File.ReadAllLines(rutaCompleta, Encoding.UTF8);
                    for (int i = 1; i < lineas.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lineas[i])) continue;

                        var partes = lineas[i].Split(';');
                        if (partes.Length >= 8)
                        {
                            decimal.TryParse(partes[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto);
                            decimal.TryParse(partes[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal montoUSD);

                            // LECTURA DIRECTA DE LA PRIORIDAD REAL EN LA POSICIÓN 7 DEL CSV
                            int prioridad = 1;
                            if (int.TryParse(partes[7].Trim(), out int prioLeida))
                            {
                                prioridad = prioLeida;
                            }

                            string estDecision = partes.Length >= 10 ? partes[9].Trim() : "Pendiente";
                            string statusOperativo = partes.Length >= 11 ? partes[10].Trim() : "UnderReview";

                            var tx = new Transaction
                            {
                                TransactionId = partes[0].Trim(),
                                SourceAccountNumber = partes[1].Trim(),
                                Amount = monto,
                                Currency = partes[3].Trim(),
                                AmountInUSD = montoUSD,
                                DestinationCurrency = partes[5].Trim(),
                                PrioridadAsignada = prioridad, // ASIGNACIÓN DE PRIORIDAD REAL DEL CSV
                                Status = statusOperativo,
                                SourceHolderName = "Empresa Titular",
                                DestinationBankName = "Banco Internacional",
                                DestinationSwiftCode = "SWIFTGLOBAL01",
                                DestinationAccountNumber = "ACC-DEST-001",
                                DestinationHolderName = "Beneficiario Destino"
                            };

                            DateTime.TryParse(partes[8].Trim(), out DateTime timestampObs);

                            lista.Add(new OperacionObservadaDto
                            {
                                Transaction = tx,
                                MotivoObservacion = partes[6].Trim().Trim('"'),
                                TimestampObservada = timestampObs != default ? timestampObs : fecha,
                                EstadoDecision = string.IsNullOrEmpty(estDecision) ? "Pendiente" : estDecision,
                                StatusTransaccion = string.IsNullOrEmpty(statusOperativo) ? "UnderReview" : statusOperativo
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al cargar operaciones observadas: {ex.Message}");
                }

                return lista;
            }
        }

        public static bool ActualizarEstadoOperacionObservada(string transactionId, string nuevoEstado, DateTime fecha, string sustento, bool? esPrueba = null)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
                    string carpetaTarget = ObtenerCarpetaDestino(esPruebaEfectivo);
                    string fechaStr = fecha.ToString("yyyy-MM-dd");
                    string nombreArchivo = esPruebaEfectivo ? $"datalog_prueba_observadas_{fechaStr}.csv" : $"log_operaciones_observadas_{fechaStr}.csv";
                    string rutaCompleta = Path.Combine(carpetaTarget, nombreArchivo);

                    if (!File.Exists(rutaCompleta)) return false;

                    var lineas = File.ReadAllLines(rutaCompleta, Encoding.UTF8).ToList();
                    bool actualizado = false;

                    for (int i = 1; i < lineas.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lineas[i])) continue;

                        var partes = lineas[i].Split(';');
                        if (partes.Length >= 8 && partes[0].Trim().Equals(transactionId, StringComparison.OrdinalIgnoreCase))
                        {
                            partes[6] = $"\"{sustento}\"";

                            string decision = nuevoEstado == "Processed" ? "Aprobado" : (nuevoEstado == "Rejected" ? "Rechazado" : nuevoEstado);

                            if (partes.Length >= 11)
                            {
                                partes[9] = decision;
                                partes[10] = nuevoEstado;
                                lineas[i] = string.Join(";", partes);
                            }
                            else
                            {
                                lineas[i] = $"{string.Join(";", partes)};{decision};{nuevoEstado}";
                            }
                            actualizado = true;
                            break;
                        }
                    }

                    if (actualizado)
                    {
                        File.WriteAllLines(rutaCompleta, lineas, Encoding.UTF8);
                    }

                    return actualizado;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al actualizar estado de operación observada: {ex.Message}");
                    return false;
                }
            }
        }

        public static void RegistrarDecisionEjecutivo(
            string transactionId,
            string sourceAccount,
            decimal amount,
            string currency,
            string destinationCurrency,
            string ejecutivoId,
            string estadoDecision,
            string detalleMotivo,
            DateTime timestampDecision,
            bool? esPrueba = null,
            int prioridadAsignada = 1)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
                    string carpetaTarget = ObtenerCarpetaDestino(esPruebaEfectivo);
                    string fechaStr = timestampDecision.ToString("yyyy-MM-dd");
                    string nombreArchivo = esPruebaEfectivo ? $"datalog_prueba_decisiones_{fechaStr}.csv" : $"log_decisiones_ejecutivos_{fechaStr}.csv";
                    string rutaCompleta = Path.Combine(carpetaTarget, nombreArchivo);

                    bool archivoNuevo = !File.Exists(rutaCompleta);

                    using (var sw = new StreamWriter(rutaCompleta, true, Encoding.UTF8))
                    {
                        if (archivoNuevo)
                        {
                            sw.WriteLine("TransactionId;SourceAccountNumber;Amount;Currency;DestinationCurrency;EjecutivoId;EstadoDecision;DetalleMotivo;PrioridadAsignada;TimestampUtc_[yyyy-MM-dd_HH:mm:ss.fffffff];Acknowledged");
                        }

                        string timestampFormatted = timestampDecision.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                        string linea = $"{transactionId};{sourceAccount};{amount};{currency};{destinationCurrency};{ejecutivoId};{estadoDecision};\"{detalleMotivo}\";{prioridadAsignada};{timestampFormatted};false";

                        sw.WriteLine(linea);
                    }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error de E/S al registrar la decisión del ejecutivo: {ex.Message}");
                    DateTime fechaActual = ConfiguracionOperativa.ObtenerFechaHoraActual();
                    RegistrarExcepcionSistema("ArchivoManager", $"Error E/S DecisionEjecutivo: {ex.Message}", transactionId, fechaActual, esPrueba, prioridadAsignada);
                    throw new InvalidOperationException("No se pudo guardar la decisión del ejecutivo debido a un bloqueo de E/S.", ex);
                }
            }
        }

        public static void RegistrarSwiftSalida(
            Transaction tx,
            string swiftGenerado,
            DateTime timestampSwift,
            bool esEmisionEjecutivo = false,
            DateTime? timestampEjecutivo = null,
            bool? esPrueba = null)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
                    string carpetaTarget = ObtenerCarpetaDestino(esPruebaEfectivo);
                    string fechaStr = timestampSwift.ToString("yyyy-MM-dd");
                    string nombreArchivo = esPruebaEfectivo ? $"datalog_prueba_swift_{fechaStr}.csv" : $"log_swift_salida_{fechaStr}.csv";
                    string rutaCompleta = Path.Combine(carpetaTarget, nombreArchivo);

                    bool archivoNuevo = !File.Exists(rutaCompleta);

                    using (var sw = new StreamWriter(rutaCompleta, true, Encoding.UTF8))
                    {
                        if (archivoNuevo)
                        {
                            sw.WriteLine("TransactionId;Amount;Currency;AmountInUSD;DestinationCurrency;DestinationSwiftCode;DestinationAccountNumber;SwiftPayload;SwiftEmisionInternaTimestamp;SwiftTransmisionEfectivaTimestamp;EstadoTransmision;PrioridadAsignada;TimestampUtc_[yyyy-MM-dd_HH:mm:ss.fffffff];SwiftEjecutivo;TimestampEjecutivo");
                        }

                        string timestampFormatted = timestampSwift.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                        string emisionInternaStr = tx.SwiftEmisionInternaTimestamp.HasValue ? tx.SwiftEmisionInternaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty;
                        string transmisionEfectivaStr = tx.SwiftTransmisionEfectivaTimestamp.HasValue ? tx.SwiftTransmisionEfectivaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty;

                        string timestampEjecutivoFormatted = timestampEjecutivo.HasValue
                            ? timestampEjecutivo.Value.ToString("yyyy-MM-dd HH:mm:ss.fffffff")
                            : string.Empty;

                        string swiftPlanificado = swiftGenerado.Replace(Environment.NewLine, " | ");
                        string amountUsdStr = tx.AmountInUSD.ToString(CultureInfo.InvariantCulture);

                        string linea = $"{tx.TransactionId};{tx.Amount};{tx.Currency};{amountUsdStr};{tx.DestinationCurrency};{tx.DestinationSwiftCode};{tx.DestinationAccountNumber};\"{swiftPlanificado}\";{emisionInternaStr};{transmisionEfectivaStr};{tx.EstadoTransmision};{tx.PrioridadAsignada};{timestampFormatted};{esEmisionEjecutivo.ToString().ToLower()};{timestampEjecutivoFormatted}";

                        sw.WriteLine(linea);
                    }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error de E/S al registrar SWIFT de salida: {ex.Message}");
                    DateTime fechaActual = ConfiguracionOperativa.ObtenerFechaHoraActual();
                    RegistrarExcepcionSistema("ArchivoManager", $"Error E/S SwiftSalida: {ex.Message}", tx.TransactionId, fechaActual, esPrueba, tx.PrioridadAsignada);
                    throw new InvalidOperationException("No se pudo persistir el registro SWIFT de salida.", ex);
                }
            }
        }

        public static void RegistrarExcepcionSistema(
            string origenModulo,
            string mensajeError,
            string transactionId,
            DateTime timestampExcepcion,
            bool? esPrueba = null,
            int prioridadAsignada = 1)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
                    string carpetaTarget = ObtenerCarpetaDestino(esPruebaEfectivo);
                    string fechaStr = timestampExcepcion.ToString("yyyy-MM-dd");
                    string nombreArchivo = esPruebaEfectivo ? $"datalog_prueba_excepciones_{fechaStr}.csv" : $"log_excepciones_sistema_{fechaStr}.csv";
                    string rutaCompleta = Path.Combine(carpetaTarget, nombreArchivo);

                    bool archivoNuevo = !File.Exists(rutaCompleta);

                    using (var sw = new StreamWriter(rutaCompleta, true, Encoding.UTF8))
                    {
                        if (archivoNuevo)
                        {
                            sw.WriteLine("TimestampUtc_[yyyy-MM-dd_HH:mm:ss.fffffff];OrigenModulo;TransactionId;MensajeError;PrioridadAsignada;Acknowledged");
                        }

                        string timestampFormatted = timestampExcepcion.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                        string errorLimpio = mensajeError.Replace("\"", "'").Replace(Environment.NewLine, " ");
                        string linea = $"{timestampFormatted};{origenModulo};{transactionId};\"{errorLimpio}\";{prioridadAsignada};false";

                        sw.WriteLine(linea);
                    }
                }
                catch
                {
                    // Fallo crítico de emergencia para evitar bucles infinitos
                }
            }
        }

        public static string[] LeerLineasArchivo(string nombreArchivo, bool? esPrueba = null)
        {
            lock (ObjetoBloqueo)
            {
                try
                {
                    bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
                    string carpetaObjetivo = esPruebaEfectivo ? CarpetaPruebas : CarpetaDatos;
                    string rutaCompleta = Path.Combine(carpetaObjetivo, nombreArchivo);
                    if (File.Exists(rutaCompleta))
                    {
                        return File.ReadAllLines(rutaCompleta, Encoding.UTF8);
                    }
                    return Array.Empty<string>();
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al leer el archivo {nombreArchivo}: {ex.Message}");
                    return Array.Empty<string>();
                }
            }
        }

        public static List<Transaction> CargarSolicitudesDiarias(DateTime fecha, bool? esPrueba = null)
        {
            bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;
            string nombreArchivo = esPruebaEfectivo ? $"datalog_prueba_solicitadas_{fecha:yyyy-MM-dd}.csv" : $"log_transacciones_solicitadas_{fecha:yyyy-MM-dd}.csv";
            var lineas = LeerLineasArchivo(nombreArchivo, esPruebaEfectivo);
            var listaTransacciones = new List<Transaction>();

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;

                var partes = lineas[i].Split(';');
                if (partes.Length >= 22)
                {
                    if (decimal.TryParse(partes[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto))
                    {
                        string moneda = partes[2].Trim();
                        decimal montoUsd = decimal.TryParse(partes[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal mUsd) ? mUsd : monto;

                        var tx = new Transaction
                        {
                            TransactionId = partes[0].Trim(),
                            Amount = monto,
                            Currency = moneda,
                            AmountInUSD = montoUsd,
                            SourceAccountNumber = partes[4].Trim(),
                            SourceHolderName = partes[5].Trim().Trim('"'),
                            DestinationBankName = partes[6].Trim(),
                            DestinationSwiftCode = partes[7].Trim(),
                            DestinationAccountNumber = partes[8].Trim(),
                            DestinationCurrency = partes[9].Trim(),
                            DestinationHolderName = partes[10].Trim().Trim('"')
                        };

                        if (DateTime.TryParse(partes[11].Trim(), out DateTime execTime)) tx.ExecutionTimestamp = execTime;
                        if (DateTime.TryParse(partes[12].Trim(), out DateTime pTime)) tx.PriorityEvaluationTimestamp = pTime;
                        if (DateTime.TryParse(partes[13].Trim(), out DateTime fTime)) tx.FrequencyEvaluationTimestamp = fTime;
                        if (DateTime.TryParse(partes[14].Trim(), out DateTime mTime)) tx.ManualReviewTimestamp = mTime;
                        if (DateTime.TryParse(partes[15].Trim(), out DateTime aTime)) tx.ApprovalTimestamp = aTime;
                        if (DateTime.TryParse(partes[16].Trim(), out DateTime emiTime)) tx.SwiftEmisionInternaTimestamp = emiTime;
                        if (DateTime.TryParse(partes[17].Trim(), out DateTime transTime)) tx.SwiftTransmisionEfectivaTimestamp = transTime;

                        tx.EstadoTransmision = partes.Length > 18 ? partes[18].Trim() : "Pendiente";
                        tx.Status = partes.Length > 19 ? partes[19].Trim() : "Pending";
                        tx.ExecutedByUserId = partes.Length > 20 && !string.IsNullOrWhiteSpace(partes[20]) ? partes[20].Trim() : null;
                        tx.ApprovedByUserId = partes.Length > 21 && !string.IsNullOrWhiteSpace(partes[21]) ? partes[21].Trim() : null;
                        tx.RiskFlag = partes.Length > 22 ? partes[22].Trim() : "Normal";

                        if (partes.Length >= 24 && int.TryParse(partes[23].Trim(), out int prioAsignada))
                        {
                            tx.PrioridadAsignada = prioAsignada;
                        }
                        else
                        {
                            tx.PrioridadAsignada = 1;
                        }

                        listaTransacciones.Add(tx);
                    }
                }
            }

            return listaTransacciones;
        }

        public static bool MarcarAcknowledgeTransaccion(string transactionId, string observacion, int anio, int mes, bool? esPrueba = null)
        {
            lock (ObjetoBloqueo)
            {
                bool seActualizo = false;
                bool esPruebaEfectivo = esPrueba ?? ConfiguracionOperativa.ModoPruebasActivo;

                List<string> carpetasBusqueda = new List<string>();
                string carpetaTarget = esPruebaEfectivo ? CarpetaPruebas : CarpetaDatos;
                if (Directory.Exists(carpetaTarget))
                {
                    carpetasBusqueda.Add(carpetaTarget);
                }

                string txTarget = transactionId.Trim();

                foreach (var carpeta in carpetasBusqueda)
                {
                    var archivosDec = Directory.GetFiles(carpeta, "*.csv");
                    foreach (var archivo in archivosDec)
                    {
                        if (!archivo.Contains("decisiones") && !archivo.Contains("excepciones")) continue;

                        var lineas = File.ReadAllLines(archivo, Encoding.UTF8).ToList();
                        bool archivoModificado = false;

                        for (int i = 1; i < lineas.Count; i++)
                        {
                            if (string.IsNullOrWhiteSpace(lineas[i])) continue;

                            var partes = lineas[i].Split(';');
                            if (partes.Length > 0 && partes[0].Trim().Equals(txTarget, StringComparison.OrdinalIgnoreCase))
                            {
                                int idxAck = partes.Length - 1;
                                partes[idxAck] = "true";

                                lineas[i] = string.Join(";", partes);
                                archivoModificado = true;
                                seActualizo = true;
                                break;
                            }
                        }

                        if (archivoModificado)
                        {
                            File.WriteAllLines(archivo, lineas, Encoding.UTF8);
                            return true;
                        }
                    }
                }

                return seActualizo;
            }
        }
    }
}