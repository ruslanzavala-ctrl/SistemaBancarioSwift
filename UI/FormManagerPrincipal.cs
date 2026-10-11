using SistemaBancario.Models;
using SistemaBancario.Persistence;
using SistemaBancario.Reporting;
using SistemaBancario.Security;
using SistemaBancario.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SistemaBancario.UI
{
    public partial class FormManagerPrincipal : Form
    {
        private Label lblSaludoBienvenida = null!;
        private Label lblRelojHeader = null!;
        private Button btnMenuSupervision = null!;
        private Button btnMenuHistorico = null!;
        private Button btnMenuKpis = null!;
        private Button btnMenuGestionSwift = null!;
        private Button btnMenuTipoCambio = null!;
        private Button btnMenuSalir = null!;

        private Panel pnlVistaSupervision = null!;
        private Panel pnlVistaHistorico = null!;
        private Panel pnlVistaKpis = null!;
        private Panel pnlVistaGestionSwift = null!;
        private Panel pnlVistaTipoCambio = null!;

        private DataGridView dgvSupervisionReal = null!;
        private Button btnRefrescarSupervision = null!;
        private Button btnLimpiarFiltrosSupervision = null!;
        private DataTable? dtSupervisionOriginal;

        private DateTimePicker dtpFechaInicioHist = null!;
        private DateTimePicker dtpFechaFinHist = null!;
        private ComboBox cmbOpcionReporteHist = null!;
        private Button btnBuscarHistorico = null!;
        private Button btnExportarHistorico = null!;
        private Button btnLimpiarFiltrosHist = null!;
        private DataGridView dgvHistoricoManager = null!;
        private DataTable? dtHistoricoOriginal;

        private ComboBox cmbModoVistaKpi = null!;
        private ComboBox cmbFiltroEjecutivoKpi = null!;
        private Label lblKpiTotalTxs = null!;
        private Label lblKpiMontoTotal = null!;
        private Label lblKpiTasaEfectividad = null!;
        private Label lblKpiPorcentajeExcepciones = null!;
        private DataGridView dgvResumenKpisGerencial = null!;
        private PanelGraficoKpi panelGraficoEstado = null!;
        private PanelGraficoPieMonedas panelGraficoMonedas = null!;

        private TabControl tabGestionSwift = null!;
        private DataGridView dgvColaEmisionSwift = null!;
        private DataTable? dtColaEmisionOriginal;

        private DataGridView dgvColaRevisionManual = null!;
        private DataTable? dtColaRevisionOriginal;

        private Button btnAprobarGerencial = null!;
        private Button btnRechazarGerencial = null!;
        private Button btnForzarSwiftGerencial = null!;
        private Button btnRefrescarGestion = null!;
        private Button btnLimpiarFiltrosGestion = null!;

        private NumericUpDown nudTipoCambioUsdPen = null!;
        private NumericUpDown nudTipoCambioEurPen = null!;
        private Label lblTasaTrianguladaUsdEur = null!;
        private Label lblUltimoManagerRegistro = null!;
        private Button btnGuardarTipoCambio = null!;

        private System.Windows.Forms.Timer timerRelojSistema = null!;
        private bool esCargaInicial = true;

        public FormManagerPrincipal()
        {
            InitializeComponentConfigurado();
            InicializarReloj();

            this.Shown += (s, e) =>
            {
                esCargaInicial = false;
                CargarDatosIniciales();
            };
        }

        private void ConfigurarFilaHeadersNumerados(DataGridView dgv)
        {
            dgv.RowHeadersWidth = 50;
            dgv.RowPostPaint += (s, e) =>
            {
                var grid = s as DataGridView;
                if (grid == null) return;
                string rowIdx = (e.RowIndex + 1).ToString();
                var centerFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
                e.Graphics.DrawString(rowIdx, grid.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
            };
        }

        private void CargarDatosIniciales()
        {
            CargarSupervisionTiempoReal();
            EjecutarConsultaHistorico();
            CalcularKpisGerenciales();
            CargarPendientesGestion();
            CargarParametrosTipoCambio();
        }

        private void InitializeComponentConfigurado()
        {
            this.Text = "Sistema Bancario - Consola Gerencial (Mánager)";
            this.Size = new Size(1720, 860);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);

            string usuario = AuthManager.UsuarioActual?.Username ?? "gerente_test";
            string rol = AuthManager.UsuarioActual?.Role.ToString() ?? "Manager";

            Panel pnlHeader = new Panel() { Size = new Size(1680, 65), Location = new Point(12, 12), BackColor = Color.FromArgb(24, 43, 73) };
            lblSaludoBienvenida = new Label() { Text = $"Consola Gerencial | Mánager: {usuario} | Rol: {rol}", ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(15, 20), AutoSize = true };
            lblRelojHeader = new Label() { Text = "Reloj Sistema...", ForeColor = Color.Yellow, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(1280, 22), AutoSize = true };
            pnlHeader.Controls.Add(lblSaludoBienvenida);
            pnlHeader.Controls.Add(lblRelojHeader);
            this.Controls.Add(pnlHeader);

            Panel pnlNavegacion = new Panel() { Size = new Size(1680, 50), Location = new Point(12, 85), BackColor = Color.Gainsboro };
            btnMenuSupervision = new Button() { Text = "1. 🔍 Supervisión Real", Size = new Size(240, 38), Location = new Point(10, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuSupervision.Click += (s, e) => MostrarVista("SUPERVISION");

            btnMenuHistorico = new Button() { Text = "2. 📅 Consolidado Histórico", Size = new Size(260, 38), Location = new Point(260, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuHistorico.Click += (s, e) => MostrarVista("HISTORICO");

            btnMenuKpis = new Button() { Text = "3. 📈 Tablero KPIs", Size = new Size(240, 38), Location = new Point(530, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuKpis.Click += (s, e) => MostrarVista("KPIS");

            btnMenuGestionSwift = new Button() { Text = "4. 💳 Gestión de SWIFTs", Size = new Size(260, 38), Location = new Point(780, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuGestionSwift.Click += (s, e) => MostrarVista("GESTION_SWIFT");

            btnMenuTipoCambio = new Button() { Text = "5. 💵 Tipo de Cambio", Size = new Size(260, 38), Location = new Point(1050, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuTipoCambio.Click += (s, e) => MostrarVista("TIPO_CAMBIO");

            btnMenuSalir = new Button() { Text = "6. 🚪 Salir", Size = new Size(180, 38), Location = new Point(1480, 6), BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuSalir.Click += BtnMenuSalir_Click;

            pnlNavegacion.Controls.Add(btnMenuSupervision);
            pnlNavegacion.Controls.Add(btnMenuHistorico);
            pnlNavegacion.Controls.Add(btnMenuKpis);
            pnlNavegacion.Controls.Add(btnMenuGestionSwift);
            pnlNavegacion.Controls.Add(btnMenuTipoCambio);
            pnlNavegacion.Controls.Add(btnMenuSalir);
            this.Controls.Add(pnlNavegacion);

            ConstruirVistaSupervision();
            ConstruirVistaHistorico();
            ConstruirVistaKpis();
            ConstruirVistaGestionSwift();
            ConstruirVistaTipoCambio();

            MostrarVista("SUPERVISION");
        }

        private void InicializarReloj()
        {
            timerRelojSistema = new System.Windows.Forms.Timer { Interval = 1000 };
            timerRelojSistema.Tick += (s, e) =>
            {
                DateTime fecha = ConfiguracionOperativa.ObtenerFechaHoraActual();
                string modo = ConfiguracionOperativa.UsarHoraUtc ? "UTC" : "LOCAL";
                lblRelojHeader.Text = $"Reloj Sistema [{modo}]: {fecha:yyyy-MM-dd HH:mm:ss}";
            };
            timerRelojSistema.Start();
        }

        private void MostrarVista(string vista)
        {
            pnlVistaSupervision.Visible = (vista == "SUPERVISION");
            pnlVistaHistorico.Visible = (vista == "HISTORICO");
            pnlVistaKpis.Visible = (vista == "KPIS");
            pnlVistaGestionSwift.Visible = (vista == "GESTION_SWIFT");
            pnlVistaTipoCambio.Visible = (vista == "TIPO_CAMBIO");

            if (!esCargaInicial)
            {
                if (vista == "SUPERVISION") CargarSupervisionTiempoReal();
                if (vista == "HISTORICO") EjecutarConsultaHistorico();
                if (vista == "KPIS") CalcularKpisGerenciales();
                if (vista == "GESTION_SWIFT") CargarPendientesGestion();
                if (vista == "TIPO_CAMBIO") CargarParametrosTipoCambio();
            }
        }

        private void ConstruirVistaSupervision()
        {
            pnlVistaSupervision = new Panel() { Size = new Size(1680, 650), Location = new Point(12, 142) };
            GroupBox gpxReal = new GroupBox() { Text = "Monitoreo Operativo en Tiempo Real (Cola 1 / En Proceso)", Location = new Point(5, 5), Size = new Size(1670, 635), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            dgvSupervisionReal = new DataGridView() { Location = new Point(15, 30), Size = new Size(1640, 540), AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgvSupervisionReal);
            dgvSupervisionReal.ColumnHeaderMouseClick += (s, e) => FiltrarGrillaGenerica(dgvSupervisionReal, dtSupervisionOriginal, e);
            gpxReal.Controls.Add(dgvSupervisionReal);

            btnRefrescarSupervision = new Button() { Text = "🔄 Refrescar Monitoreo", Location = new Point(15, 580), Size = new Size(800, 42), BackColor = Color.LightSkyBlue, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnRefrescarSupervision.Click += (s, e) => CargarSupervisionTiempoReal();
            gpxReal.Controls.Add(btnRefrescarSupervision);

            btnLimpiarFiltrosSupervision = new Button() { Text = "🧹 Limpiar Filtros", Location = new Point(825, 580), Size = new Size(830, 42), BackColor = Color.LightGray, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnLimpiarFiltrosSupervision.Click += (s, e) => LimpiarFiltroGenerico(dgvSupervisionReal, dtSupervisionOriginal);
            gpxReal.Controls.Add(btnLimpiarFiltrosSupervision);

            pnlVistaSupervision.Controls.Add(gpxReal);
            this.Controls.Add(pnlVistaSupervision);
        }

        private void CargarSupervisionTiempoReal()
        {
            try
            {
                DateTime fechaSistema = ConfiguracionOperativa.ObtenerFechaHoraActual();
                var r = ReporteManager.ObtenerReporteOperacionActual(fechaSistema, OpcionReporteOperacionActual.SoloDetalleTransacciones);

                dtSupervisionOriginal = new DataTable();
                dtSupervisionOriginal.Columns.Add("ID", typeof(string));
                dtSupervisionOriginal.Columns.Add("FechaEmisionSolicitud", typeof(string));
                dtSupervisionOriginal.Columns.Add("Origen", typeof(string));
                dtSupervisionOriginal.Columns.Add("Titular", typeof(string));
                dtSupervisionOriginal.Columns.Add("Monto Original", typeof(string));
                dtSupervisionOriginal.Columns.Add("Monto (USD)", typeof(string));
                dtSupervisionOriginal.Columns.Add("BancoDestino", typeof(string));
                dtSupervisionOriginal.Columns.Add("CuentaDestino", typeof(string));
                dtSupervisionOriginal.Columns.Add("EstadoProceso", typeof(string));
                dtSupervisionOriginal.Columns.Add("EstadoTransmisionSWIFT", typeof(string));
                dtSupervisionOriginal.Columns.Add("Prioridad", typeof(int));

                // MONITOREO EN TIEMPO REAL: Se excluyen operaciones completadas o rechazadas.
                // Se ordena por Prioridad (8 -> 6 -> 4 -> 2) y por Orden de Llegada (HoraIngreso).
                var listaSupervision = (r.DetalleTransacciones ?? new List<TransaccionOperacionActualDto>())
                    .Where(t => t.EstadoTransmision != "TransmitidoEfectivo" && t.StatusActual != "Rejected")
                    .OrderByDescending(t => t.PrioridadAsignada)
                    .ThenBy(t => t.HoraIngreso)
                    .ToList();

                foreach (var t in listaSupervision)
                {
                    dtSupervisionOriginal.Rows.Add(
                        t.TransactionId,
                        t.HoraIngreso.ToString("yyyy-MM-dd HH:mm:ss"),
                        t.SourceAccountNumber,
                        t.SourceHolderName,
                        $"{t.Amount:N2} {t.Currency}",
                        $"$ {t.AmountInUSD:N2}",
                        t.DestinationBankName,
                        t.DestinationAccountNumber,
                        t.StatusActual,
                        t.EstadoTransmision,
                        t.PrioridadAsignada
                    );
                }

                dgvSupervisionReal.DataSource = dtSupervisionOriginal.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar monitoreo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConstruirVistaHistorico()
        {
            pnlVistaHistorico = new Panel() { Size = new Size(1680, 650), Location = new Point(12, 142) };
            GroupBox gpxHist = new GroupBox() { Text = "Consulta Consolidada por Rango de Fechas (Mismo Mes)", Location = new Point(5, 5), Size = new Size(1670, 635), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            int xPos = 20;
            gpxHist.Controls.Add(new Label() { Text = "Fecha Inicio:", Location = new Point(xPos, 30), AutoSize = true });
            dtpFechaInicioHist = new DateTimePicker() { Location = new Point(xPos, 55), Size = new Size(140, 25), Format = DateTimePickerFormat.Short };
            DateTime fechaSistema = ConfiguracionOperativa.ObtenerFechaHoraActual();
            dtpFechaInicioHist.Value = new DateTime(fechaSistema.Year, fechaSistema.Month, 1);
            gpxHist.Controls.Add(dtpFechaInicioHist);

            xPos += 155;
            gpxHist.Controls.Add(new Label() { Text = "Fecha Fin:", Location = new Point(xPos, 30), AutoSize = true });
            dtpFechaFinHist = new DateTimePicker() { Location = new Point(xPos, 55), Size = new Size(140, 25), Format = DateTimePickerFormat.Short };
            dtpFechaFinHist.Value = fechaSistema;
            gpxHist.Controls.Add(dtpFechaFinHist);

            xPos += 155;
            gpxHist.Controls.Add(new Label() { Text = "Contenido Consulta:", Location = new Point(xPos, 30), AutoSize = true });
            cmbOpcionReporteHist = new ComboBox() { Location = new Point(xPos, 55), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbOpcionReporteHist.Items.AddRange(new object[] { "Solo Transacciones", "Solo Indicadores (KPIs)", "Ambos (Detalle + KPIs)" });
            cmbOpcionReporteHist.SelectedIndex = 0;
            gpxHist.Controls.Add(cmbOpcionReporteHist);

            xPos += 215;
            btnBuscarHistorico = new Button() { Text = "🔍 Consultar Rango", Location = new Point(xPos, 53), Size = new Size(150, 30), BackColor = Color.LightSkyBlue, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnBuscarHistorico.Click += (s, e) => EjecutarConsultaHistorico();
            gpxHist.Controls.Add(btnBuscarHistorico);

            xPos += 160;
            btnLimpiarFiltrosHist = new Button() { Text = "🧹 Limpiar Filtros", Location = new Point(xPos, 53), Size = new Size(140, 30), BackColor = Color.LightGray, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnLimpiarFiltrosHist.Click += (s, e) => LimpiarFiltroGenerico(dgvHistoricoManager, dtHistoricoOriginal);
            gpxHist.Controls.Add(btnLimpiarFiltrosHist);

            dgvHistoricoManager = new DataGridView() { Location = new Point(20, 95), Size = new Size(1630, 475), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgvHistoricoManager);
            dgvHistoricoManager.ColumnHeaderMouseClick += (s, e) => FiltrarGrillaGenerica(dgvHistoricoManager, dtHistoricoOriginal, e);
            gpxHist.Controls.Add(dgvHistoricoManager);

            btnExportarHistorico = new Button() { Text = "📊 Exportar Consolidado Histórico (CSV)", Location = new Point(20, 580), Size = new Size(1630, 40), BackColor = Color.Gainsboro, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnExportarHistorico.Click += BtnExportarHistorico_Click;
            gpxHist.Controls.Add(btnExportarHistorico);

            pnlVistaHistorico.Controls.Add(gpxHist);
            this.Controls.Add(pnlVistaHistorico);
        }

        private void EjecutarConsultaHistorico()
        {
            try
            {
                OpcionReporteOperacionActual opcion = (OpcionReporteOperacionActual)(cmbOpcionReporteHist.SelectedIndex + 1);
                var r = ReporteManagerAcumulado.ObtenerReporteRangoFechas(dtpFechaInicioHist.Value, dtpFechaFinHist.Value, opcion);

                if (r.DetalleTransacciones != null)
                {
                    dtHistoricoOriginal = new DataTable();
                    dtHistoricoOriginal.Columns.Add("ID", typeof(string));
                    dtHistoricoOriginal.Columns.Add("FechaEmisionSolicitud", typeof(string));
                    dtHistoricoOriginal.Columns.Add("SwiftEmisionInterna", typeof(string));
                    dtHistoricoOriginal.Columns.Add("SwiftTransmisionEfectiva", typeof(string));
                    dtHistoricoOriginal.Columns.Add("Origen", typeof(string));
                    dtHistoricoOriginal.Columns.Add("Titular", typeof(string));
                    dtHistoricoOriginal.Columns.Add("Monto Original", typeof(string));
                    dtHistoricoOriginal.Columns.Add("Monto (USD)", typeof(string));
                    dtHistoricoOriginal.Columns.Add("Destino", typeof(string));
                    dtHistoricoOriginal.Columns.Add("Prioridad", typeof(int));
                    dtHistoricoOriginal.Columns.Add("EstadoProceso", typeof(string));
                    dtHistoricoOriginal.Columns.Add("EstadoTransmisionSWIFT", typeof(string));
                    dtHistoricoOriginal.Columns.Add("Excepcion", typeof(string));
                    dtHistoricoOriginal.Columns.Add("Acknowledged", typeof(string));

                    foreach (var t in r.DetalleTransacciones)
                    {
                        dtHistoricoOriginal.Rows.Add(
                            t.TransactionId,
                            t.HoraIngreso.ToString("yyyy-MM-dd HH:mm:ss"),
                            t.SwiftEmisionInternaTimestamp.HasValue ? t.SwiftEmisionInternaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Pendiente",
                            t.SwiftTransmisionEfectivaTimestamp.HasValue ? t.SwiftTransmisionEfectivaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Programada",
                            t.SourceAccountNumber,
                            t.SourceHolderName,
                            $"{t.Amount:N2} {t.Currency}",
                            $"$ {t.AmountInUSD:N2}",
                            t.DestinationAccountNumber,
                            t.PrioridadAsignada,
                            t.StatusActual,
                            t.EstadoTransmision,
                            t.HasExceptionFlag ? "SÍ" : "NO",
                            t.Acknowledged ? "CONFIRMADO" : "PENDIENTE"
                        );
                    }
                    dgvHistoricoManager.DataSource = dtHistoricoOriginal.DefaultView;
                }
                else
                {
                    dgvHistoricoManager.DataSource = null;
                    dtHistoricoOriginal = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar acumulado histórico: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportarHistorico_Click(object? sender, EventArgs e)
        {
            using SaveFileDialog sfd = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"Consolidado_Gerencial_{ConfiguracionOperativa.ObtenerFechaHoraActual():yyyyMMdd_HHmmss}.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                    sw.WriteLine("ID;FechaEmisionSolicitud;SwiftEmisionInterna;SwiftTransmisionEfectiva;Origen;Titular;MontoOriginal;MontoUSD;Destino;Prioridad;EstadoProceso;EstadoTransmisionSWIFT;Excepcion;Acknowledged");
                    foreach (DataGridViewRow row in dgvHistoricoManager.Rows)
                    {
                        if (!row.IsNewRow)
                        {
                            var cells = row.Cells;
                            sw.WriteLine($"{cells[0].Value};{cells[1].Value};{cells[2].Value};{cells[3].Value};{cells[4].Value};\"{cells[5].Value}\";{cells[6].Value};{cells[7].Value};{cells[8].Value};{cells[9].Value};{cells[10].Value};{cells[11].Value};{cells[12].Value};{cells[13].Value}");
                        }
                    }
                    MessageBox.Show("Consolidado histórico exportado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ConstruirVistaKpis()
        {
            pnlVistaKpis = new Panel() { Size = new Size(1680, 650), Location = new Point(12, 142) };
            Panel pnlFiltrosKpi = new Panel() { Size = new Size(1660, 45), Location = new Point(10, 5), BackColor = Color.Gainsboro };

            pnlFiltrosKpi.Controls.Add(new Label() { Text = "Modalidad KPIs:", Location = new Point(15, 12), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            cmbModoVistaKpi = new ComboBox() { Location = new Point(130, 9), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbModoVistaKpi.Items.AddRange(new object[] { "1) Total Consolidado Banco", "2) Individual por Ejecutivo", "3) Participación de Ejecutivos" });
            cmbModoVistaKpi.SelectedIndex = 0;
            cmbModoVistaKpi.SelectedIndexChanged += (s, e) => { if (!esCargaInicial) { cmbFiltroEjecutivoKpi.Enabled = (cmbModoVistaKpi.SelectedIndex == 1); CalcularKpisGerenciales(); } };
            pnlFiltrosKpi.Controls.Add(cmbModoVistaKpi);

            pnlFiltrosKpi.Controls.Add(new Label() { Text = "Ejecutivo:", Location = new Point(410, 12), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            cmbFiltroEjecutivoKpi = new ComboBox() { Location = new Point(480, 9), Size = new Size(220, 25), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            cmbFiltroEjecutivoKpi.SelectedIndexChanged += (s, e) => { if (!esCargaInicial) CalcularKpisGerenciales(); };
            pnlFiltrosKpi.Controls.Add(cmbFiltroEjecutivoKpi);
            pnlVistaKpis.Controls.Add(pnlFiltrosKpi);

            pnlVistaKpis.Controls.Add(CrearTarjetaKpi("Total Solicitudes", out lblKpiTotalTxs, Color.SteelBlue, 10, 60, 400));
            pnlVistaKpis.Controls.Add(CrearTarjetaKpi("Monto Procesado (USD)", out lblKpiMontoTotal, Color.SeaGreen, 430, 60, 400));
            pnlVistaKpis.Controls.Add(CrearTarjetaKpi("Tasa Efectividad Global", out lblKpiTasaEfectividad, Color.DarkGoldenrod, 850, 60, 400));
            pnlVistaKpis.Controls.Add(CrearTarjetaKpi("% Excepciones / Rechazos", out lblKpiPorcentajeExcepciones, Color.Coral, 1270, 60, 400));

            GroupBox gpxKpiTabla = new GroupBox() { Text = "Desglose por Estado Operativo", Location = new Point(10, 195), Size = new Size(540, 445), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            dgvResumenKpisGerencial = new DataGridView() { Location = new Point(15, 30), Size = new Size(510, 400), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgvResumenKpisGerencial);
            gpxKpiTabla.Controls.Add(dgvResumenKpisGerencial);
            pnlVistaKpis.Controls.Add(gpxKpiTabla);

            GroupBox gpxGraficoEstado = new GroupBox() { Text = "Gráfico 1: Transacciones por Estado (Doughnut)", Location = new Point(560, 195), Size = new Size(540, 445), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            panelGraficoEstado = new PanelGraficoKpi() { Location = new Point(15, 30), Size = new Size(510, 400), BackColor = Color.White };
            gpxGraficoEstado.Controls.Add(panelGraficoEstado);
            pnlVistaKpis.Controls.Add(gpxGraficoEstado);

            GroupBox gpxGraficoMonedas = new GroupBox() { Text = "Gráfico 2: Montos Totales en USD por Moneda (Pie)", Location = new Point(1110, 195), Size = new Size(560, 445), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            panelGraficoMonedas = new PanelGraficoPieMonedas() { Location = new Point(15, 30), Size = new Size(530, 400), BackColor = Color.White };
            gpxGraficoMonedas.Controls.Add(panelGraficoMonedas);
            pnlVistaKpis.Controls.Add(gpxGraficoMonedas);

            this.Controls.Add(pnlVistaKpis);
        }

        private Panel CrearTarjetaKpi(string titulo, out Label lblValor, Color colorFondo, int xPos, int yPos, int ancho)
        {
            Panel pnl = new Panel() { Size = new Size(ancho, 125), Location = new Point(xPos, yPos), BackColor = colorFondo };
            pnl.Controls.Add(new Label() { Text = titulo, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(15, 15), AutoSize = true });
            lblValor = new Label() { Text = "0", ForeColor = Color.White, Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(15, 50), AutoSize = true };
            pnl.Controls.Add(lblValor);
            return pnl;
        }

        private void CalcularKpisGerenciales()
        {
            try
            {
                DateTime inicio = dtpFechaInicioHist != null ? dtpFechaInicioHist.Value : new DateTime(ConfiguracionOperativa.ObtenerFechaHoraActual().Year, ConfiguracionOperativa.ObtenerFechaHoraActual().Month, 1);
                DateTime fin = dtpFechaFinHist != null ? dtpFechaFinHist.Value : ConfiguracionOperativa.ObtenerFechaHoraActual();

                var r = ReporteManagerAcumulado.ObtenerReporteRangoFechas(inicio, fin, OpcionReporteOperacionActual.Ambos);
                var listaGeneral = r.DetalleTransacciones ?? new List<TransaccionOperacionActualDto>();

                int total = listaGeneral.Count;
                decimal montoTotalUSD = r.IndicadoresGerenciales?.MontoTotalProcesadoUSD ?? 0m;
                decimal tasaEfectividad = r.IndicadoresGerenciales?.TasaEfectividadPorcentaje ?? 0m;

                int rechazadas = r.IndicadoresGerenciales?.TotalRechazadas ?? 0;
                int excepciones = r.IndicadoresGerenciales?.TotalExcepcionesSistema ?? 0;
                double pctExcepcionesRechazos = total > 0 ? (double)(rechazadas + excepciones) / total * 100.0 : 0.0;

                lblKpiTotalTxs.Text = total.ToString();
                lblKpiMontoTotal.Text = $"$ {montoTotalUSD:N2}";
                lblKpiTasaEfectividad.Text = $"{tasaEfectividad:F1}%";
                lblKpiPorcentajeExcepciones.Text = $"{pctExcepcionesRechazos:F1}%";

                var resumenGrilla = listaGeneral.GroupBy(t => t.StatusActual)
                    .Select(g => new { Estado = g.Key, Cantidad = g.Count(), Porcentaje = $"{(double)g.Count() / (total > 0 ? total : 1) * 100:F1}%", MontoUSD = g.Sum(x => x.AmountInUSD) }).ToList();

                dgvResumenKpisGerencial.DataSource = resumenGrilla;

                panelGraficoEstado.CargarDatosDoughnut(resumenGrilla.Select(x => (x.Estado, x.Cantidad)).ToList());

                var dictMonedas = r.IndicadoresGerenciales?.DistribucionMontosPorMoneda ?? new Dictionary<string, decimal>();
                panelGraficoMonedas.CargarDatosPieMonedas(dictMonedas.Select(k => (k.Key, k.Value)).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular KPIs gerenciales: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConstruirVistaGestionSwift()
        {
            pnlVistaGestionSwift = new Panel() { Size = new Size(1680, 650), Location = new Point(12, 142) };
            GroupBox gpxGestion = new GroupBox() { Text = "Bandeja Operativa Gerencial", Location = new Point(5, 5), Size = new Size(1670, 635), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            tabGestionSwift = new TabControl() { Location = new Point(15, 25), Size = new Size(1640, 530), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

            TabPage tabEmision = new TabPage("1. ⚡ Cola de Emisión SWIFT Pendiente (Cola 3)");
            dgvColaEmisionSwift = CrearGrillaConCheckbox();
            tabEmision.Controls.Add(dgvColaEmisionSwift);

            TabPage tabRevision = new TabPage("2. 🔍 Cola de Revisión Manual (Cola 2 / UnderReview)");
            dgvColaRevisionManual = CrearGrillaConCheckbox();
            tabRevision.Controls.Add(dgvColaRevisionManual);

            tabGestionSwift.TabPages.Add(tabEmision);
            tabGestionSwift.TabPages.Add(tabRevision);
            gpxGestion.Controls.Add(tabGestionSwift);

            btnAprobarGerencial = new Button() { Text = "✅ Aprobar Seleccionadas", Location = new Point(15, 565), Size = new Size(310, 50), BackColor = Color.LightGreen, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnAprobarGerencial.Click += (s, e) => ProcesarAccionGerencial("APROBAR");
            gpxGestion.Controls.Add(btnAprobarGerencial);

            btnRechazarGerencial = new Button() { Text = "❌ Rechazar Seleccionadas", Location = new Point(340, 565), Size = new Size(310, 50), BackColor = Color.LightCoral, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnRechazarGerencial.Click += (s, e) => ProcesarAccionGerencial("RECHAZAR");
            gpxGestion.Controls.Add(btnRechazarGerencial);

            btnForzarSwiftGerencial = new Button() { Text = "⚡ Forzar Transmisión SWIFT", Location = new Point(665, 565), Size = new Size(310, 50), BackColor = Color.Khaki, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnForzarSwiftGerencial.Click += (s, e) => ProcesarAccionGerencial("FORZAR_SWIFT");
            gpxGestion.Controls.Add(btnForzarSwiftGerencial);

            btnRefrescarGestion = new Button() { Text = "🔄 Refrescar Bandeja", Location = new Point(990, 565), Size = new Size(320, 50), BackColor = Color.LightSkyBlue, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnRefrescarGestion.Click += (s, e) => CargarPendientesGestion();
            gpxGestion.Controls.Add(btnRefrescarGestion);

            btnLimpiarFiltrosGestion = new Button() { Text = "🧹 Limpiar Filtros", Location = new Point(1325, 565), Size = new Size(330, 50), BackColor = Color.LightGray, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnLimpiarFiltrosGestion.Click += (s, e) =>
            {
                if (tabGestionSwift.SelectedIndex == 0) LimpiarFiltroGenerico(dgvColaEmisionSwift, dtColaEmisionOriginal);
                else LimpiarFiltroGenerico(dgvColaRevisionManual, dtColaRevisionOriginal);
            };
            gpxGestion.Controls.Add(btnLimpiarFiltrosGestion);

            pnlVistaGestionSwift.Controls.Add(gpxGestion);
            this.Controls.Add(pnlVistaGestionSwift);
        }

        private void ConstruirVistaTipoCambio()
        {
            pnlVistaTipoCambio = new Panel() { Size = new Size(1680, 650), Location = new Point(12, 142) };
            GroupBox gpxTc = new GroupBox() { Text = "Parametrización de Tipos de Cambio Centralizado (Persistencia TipoDeCambio.csv)", Location = new Point(360, 60), Size = new Size(960, 500), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            int yPos = 55;
            gpxTc.Controls.Add(new Label() { Text = "Tasa de Cambio USD → PEN (Dólares a Soles S/.):", Location = new Point(60, yPos), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Regular) });
            nudTipoCambioUsdPen = new NumericUpDown()
            {
                Location = new Point(530, yPos - 3),
                Size = new Size(230, 27),
                DecimalPlaces = 4,
                Increment = 0.0050m,
                Minimum = 1.0000m,
                Maximum = 15.0000m,
                Value = 3.7500m
            };
            nudTipoCambioUsdPen.ValueChanged += (s, e) => RecalcularLabelTriangulada();
            gpxTc.Controls.Add(nudTipoCambioUsdPen);

            yPos += 75;
            gpxTc.Controls.Add(new Label() { Text = "Tasa de Cambio EUR → PEN (Euros a Soles S/.):", Location = new Point(60, yPos), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Regular) });
            nudTipoCambioEurPen = new NumericUpDown()
            {
                Location = new Point(530, yPos - 3),
                Size = new Size(230, 27),
                DecimalPlaces = 4,
                Increment = 0.0050m,
                Minimum = 1.0000m,
                Maximum = 15.0000m,
                Value = 4.0500m
            };
            nudTipoCambioEurPen.ValueChanged += (s, e) => RecalcularLabelTriangulada();
            gpxTc.Controls.Add(nudTipoCambioEurPen);

            yPos += 75;
            lblTasaTrianguladaUsdEur = new Label()
            {
                Text = "Tasa Triangulada Calculada: 1 USD = 0.9259 EUR | 1 EUR = 1.0800 USD",
                Location = new Point(60, yPos),
                AutoSize = true,
                ForeColor = Color.DarkBlue,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            gpxTc.Controls.Add(lblTasaTrianguladaUsdEur);

            yPos += 55;
            lblUltimoManagerRegistro = new Label()
            {
                Text = "Último registro guardado por: N/A",
                Location = new Point(60, yPos),
                AutoSize = true,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic)
            };
            gpxTc.Controls.Add(lblUltimoManagerRegistro);

            yPos += 85;
            btnGuardarTipoCambio = new Button()
            {
                Text = "💾 Guardar Tasa de Cambio en Datalog (TipoDeCambio.csv)",
                Location = new Point(60, yPos),
                Size = new Size(700, 55),
                BackColor = Color.LightGreen,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            btnGuardarTipoCambio.Click += BtnGuardarTipoCambio_Click;
            gpxTc.Controls.Add(btnGuardarTipoCambio);

            pnlVistaTipoCambio.Controls.Add(gpxTc);
            this.Controls.Add(pnlVistaTipoCambio);
        }

        private void RecalcularLabelTriangulada()
        {
            decimal usdPen = nudTipoCambioUsdPen.Value;
            decimal eurPen = nudTipoCambioEurPen.Value;

            decimal usdEur = eurPen > 0 ? usdPen / eurPen : 0m;
            decimal eurUsd = usdPen > 0 ? eurPen / usdPen : 0m;

            lblTasaTrianguladaUsdEur.Text = $"Tasa Triangulada Automática: 1 USD = {usdEur:F4} EUR  |  1 EUR = {eurUsd:F4} USD";
        }

        private void CargarParametrosTipoCambio()
        {
            try
            {
                TipoCambio tc = ArchivoManager.ObtenerTipoCambioVigente();
                if (nudTipoCambioUsdPen != null && nudTipoCambioEurPen != null)
                {
                    nudTipoCambioUsdPen.Value = tc.TipoCambioUsdPen > 0 ? tc.TipoCambioUsdPen : 3.7500m;
                    nudTipoCambioEurPen.Value = tc.TipoCambioEurPen > 0 ? tc.TipoCambioEurPen : 4.0500m;
                    lblUltimoManagerRegistro.Text = string.IsNullOrEmpty(tc.ManagerUsername)
                        ? "Último registro guardado por: Configuración Inicial"
                        : $"Último registro guardado por: [{tc.ManagerUsername}] el {tc.FechaRegistro:yyyy-MM-dd HH:mm:ss}";
                    RecalcularLabelTriangulada();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar tipo de cambio: {ex.Message}");
            }
        }

        private void BtnGuardarTipoCambio_Click(object? sender, EventArgs e)
        {
            try
            {
                decimal usdPen = nudTipoCambioUsdPen.Value;
                decimal eurPen = nudTipoCambioEurPen.Value;
                string usuarioManager = AuthManager.UsuarioActual?.Username ?? "gerente_test";
                DateTime fechaActual = ConfiguracionOperativa.ObtenerFechaHoraActual();

                ArchivoManager.RegistrarHistoricoTipoCambio(usuarioManager, usdPen, eurPen, fechaActual);

                MessageBox.Show($"Tasa de cambio actualizada e ingresada al datalog TipoDeCambio.csv con éxito:\n\n" +
                                $"• Usuario Mánager: {usuarioManager}\n" +
                                $"• Fecha / Hora: {fechaActual:yyyy-MM-dd HH:mm:ss}\n" +
                                $"• 1 USD = {usdPen:F4} PEN (S/.)\n" +
                                $"• 1 EUR = {eurPen:F4} PEN (S/.)",
                                "Persistencia Actualizada",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                CargarParametrosTipoCambio();
                CalcularKpisGerenciales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el tipo de cambio: {ex.Message}", "Error de Persistencia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataGridView CrearGrillaConCheckbox()
        {
            DataGridView dgv = new DataGridView() { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgv);
            dgv.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "Seleccion", HeaderText = "☑", Width = 40, ReadOnly = false, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            return dgv;
        }

        private void CargarPendientesGestion()
        {
            try
            {
                DateTime fechaSistema = ConfiguracionOperativa.ObtenerFechaHoraActual();

                dtColaEmisionOriginal = CrearEstructuraTablaGestion();
                var listaEmisionSwift = ReporteManager.ObtenerColaSwiftPendienteTiempoReal(fechaSistema);

                foreach (var t in listaEmisionSwift)
                {
                    dtColaEmisionOriginal.Rows.Add(false, t.TransactionId, t.HoraIngreso.ToString("yyyy-MM-dd HH:mm:ss"), t.SourceAccountNumber, t.SourceHolderName, $"{t.Amount:N2} {t.Currency}", $"$ {t.AmountInUSD:N2}", t.DestinationBankName, t.DestinationAccountNumber, t.StatusActual, t.PrioridadAsignada, "Pendiente");
                }
                AsignarDataSourceConCheckbox(dgvColaEmisionSwift, dtColaEmisionOriginal);

                dtColaRevisionOriginal = CrearEstructuraTablaGestion();
                var listaObservadas = ArchivoManager.CargarOperacionesObservadas(fechaSistema);
                var listaRevisionPendientes = listaObservadas
                    .Where(o => o.EstadoDecision == "Pendiente")
                    .OrderByDescending(o => o.Transaction.PrioridadAsignada)
                    .ThenBy(o => o.TimestampObservada)
                    .ToList();

                foreach (var obs in listaRevisionPendientes)
                {
                    var t = obs.Transaction;
                    dtColaRevisionOriginal.Rows.Add(false, t.TransactionId, obs.TimestampObservada.ToString("yyyy-MM-dd HH:mm:ss"), t.SourceAccountNumber, t.SourceHolderName, $"{t.Amount:N2} {t.Currency}", $"$ {t.AmountInUSD:N2}", t.DestinationBankName, t.DestinationAccountNumber, t.Status, t.PrioridadAsignada, obs.EstadoDecision);
                }
                AsignarDataSourceConCheckbox(dgvColaRevisionManual, dtColaRevisionOriginal);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar listas de gestión de SWIFT: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable CrearEstructuraTablaGestion()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Seleccion", typeof(bool));
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("FechaEmisionSolicitud", typeof(string));
            dt.Columns.Add("Origen", typeof(string));
            dt.Columns.Add("Titular", typeof(string));
            dt.Columns.Add("Monto Original", typeof(string));
            dt.Columns.Add("Monto (USD)", typeof(string));
            dt.Columns.Add("BancoDestino", typeof(string));
            dt.Columns.Add("CuentaDestino", typeof(string));
            dt.Columns.Add("EstadoProceso", typeof(string));
            dt.Columns.Add("Prioridad", typeof(int));
            dt.Columns.Add("EstadoDecision", typeof(string));
            return dt;
        }

        private void AsignarDataSourceConCheckbox(DataGridView dgv, DataTable dt)
        {
            dgv.DataSource = dt.DefaultView;
            var colSeleccion = dgv.Columns["Seleccion"];
            if (colSeleccion != null)
            {
                colSeleccion.ReadOnly = false;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Name != "Seleccion") col.ReadOnly = true;
                }
            }
        }

        private DataGridView ObtenerGrillaActivaGestion() => tabGestionSwift.SelectedIndex == 0 ? dgvColaEmisionSwift : dgvColaRevisionManual;

        private List<string> ObtenerIdsSeleccionados(DataGridView dgv)
        {
            List<string> seleccionados = new List<string>();
            dgv.EndEdit();

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (Convert.ToBoolean(row.Cells["Seleccion"].Value ?? false))
                {
                    string id = row.Cells["ID"].Value?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(id)) seleccionados.Add(id);
                }
            }

            if (seleccionados.Count == 0 && dgv.SelectedRows.Count > 0)
            {
                string id = dgv.SelectedRows[0].Cells["ID"].Value?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(id)) seleccionados.Add(id);
            }

            return seleccionados;
        }

        private void ProcesarAccionGerencial(string accion)
        {
            DataGridView dgvActiva = ObtenerGrillaActivaGestion();
            List<string> idsProcesar = ObtenerIdsSeleccionados(dgvActiva);

            if (idsProcesar.Count == 0)
            {
                MessageBox.Show("Marque la casilla ☑ de la(s) transacción(es) a gestionar.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string usuarioGerente = AuthManager.UsuarioActual?.Username ?? "gerente_test";
            DateTime fechaHoy = ConfiguracionOperativa.ObtenerFechaHoraActual();
            var solicitudesHoy = ArchivoManager.CargarSolicitudesDiarias(fechaHoy);

            try
            {
                foreach (string txId in idsProcesar)
                {
                    var tx = solicitudesHoy.FirstOrDefault(t => t.TransactionId == txId);

                    if (accion == "APROBAR" && tx != null)
                    {
                        ArchivoManager.ActualizarEstadoOperacionObservada(txId, "Processed", fechaHoy, $"Aprobación Directa Gerencial por [{usuarioGerente}]");
                        ArchivoManager.RegistrarDecisionEjecutivo(txId, tx.SourceAccountNumber, tx.Amount, tx.Currency, tx.DestinationCurrency, usuarioGerente, "Aprobada", "Aprobación Directa Gerencial", fechaHoy, prioridadAsignada: tx.PrioridadAsignada);

                        string tramaSwift = tx.GenerarSwift(fechaHoy);
                        bool enVentana = GestorTransacciones.EsVentanaAtencionSwift(fechaHoy);

                        if (enVentana)
                        {
                            tx.EstadoTransmision = "TransmitidoEfectivo";
                            tx.SwiftTransmisionEfectivaTimestamp = fechaHoy;
                            ArchivoManager.RegistrarSwiftSalida(tx, tramaSwift, fechaHoy, esEmisionEjecutivo: true, timestampEjecutivo: fechaHoy);
                        }
                        else
                        {
                            tx.EstadoTransmision = "EncoladoParaTransmision";
                            DateTime fechaProgramada = GestorTransacciones.CalcularFechaEmisionSwift(fechaHoy);
                            tx.SwiftTransmisionEfectivaTimestamp = fechaProgramada;
                            ArchivoManager.RegistrarSwiftSalida(tx, tramaSwift, fechaProgramada, esEmisionEjecutivo: true, timestampEjecutivo: fechaHoy);
                        }
                    }
                    else if (accion == "RECHAZAR" && tx != null)
                    {
                        ArchivoManager.ActualizarEstadoOperacionObservada(txId, "Rejected", fechaHoy, $"Rechazo Directo Gerencial por [{usuarioGerente}]");
                        ArchivoManager.RegistrarDecisionEjecutivo(txId, tx.SourceAccountNumber, tx.Amount, tx.Currency, tx.DestinationCurrency, usuarioGerente, "Rechazada", "Rechazo Directo Gerencial", fechaHoy, prioridadAsignada: tx.PrioridadAsignada);
                    }
                    else if (accion == "FORZAR_SWIFT" && tx != null)
                    {
                        GestorTransacciones.ForzarEmisionSwiftPorEjecutivo(tx, usuarioGerente);
                    }
                }

                MessageBox.Show($"Acción [{accion}] procesada exitosamente sobre {idsProcesar.Count} transacción(es).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                CargarPendientesGestion();
                CargarSupervisionTiempoReal();
                CalcularKpisGerenciales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al ejecutar acción gerencial: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FiltrarGrillaGenerica(DataGridView dgv, DataTable? dtOriginal, DataGridViewCellMouseEventArgs e)
        {
            if (dtOriginal == null || e.ColumnIndex < 0) return;

            string nombreColumna = dgv.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(nombreColumna)) nombreColumna = dgv.Columns[e.ColumnIndex].Name;

            string tituloColumna = dgv.Columns[e.ColumnIndex].HeaderText;

            using Form frmFiltro = new Form { Text = $"Filtrar por [{tituloColumna}]", Size = new Size(350, 160), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            TextBox txtFiltro = new TextBox { Location = new Point(15, 40), Size = new Size(300, 23) };
            Button btnAplicar = new Button { Text = "Aplicar Filtro", Location = new Point(115, 75), Size = new Size(100, 30), DialogResult = DialogResult.OK, BackColor = Color.LightSkyBlue };

            frmFiltro.Controls.Add(new Label { Text = $"Ingrese criterio para '{tituloColumna}':", Location = new Point(15, 15), AutoSize = true });
            frmFiltro.Controls.Add(txtFiltro);
            frmFiltro.Controls.Add(btnAplicar);

            if (frmFiltro.ShowDialog() == DialogResult.OK)
            {
                string valorFiltro = txtFiltro.Text.Trim().Replace("'", "''");
                if (!string.IsNullOrEmpty(valorFiltro))
                {
                    dtOriginal.DefaultView.RowFilter = $"CONVERT([{nombreColumna}], 'System.String') LIKE '%{valorFiltro}%'";
                }
            }
        }

        private void LimpiarFiltroGenerico(DataGridView dgv, DataTable? dtOriginal)
        {
            if (dtOriginal != null) dtOriginal.DefaultView.RowFilter = string.Empty;
        }

        private void BtnMenuSalir_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("¿Está seguro de que desea salir?", "Confirmar Salida", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                AuthManager.UsuarioActual = null;
                this.Close();
            }
        }
    }

    public class PanelGraficoKpi : Panel
    {
        private List<(string Etiqueta, int Valor)> datosPuntos = new List<(string, int)>();
        private Color[] paletaColores = new Color[] { Color.SteelBlue, Color.SeaGreen, Color.Coral, Color.Goldenrod, Color.MediumPurple, Color.IndianRed };

        public PanelGraficoKpi() { this.DoubleBuffered = true; }

        public void CargarDatosDoughnut(List<(string, int)> datos)
        {
            datosPuntos = datos;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (datosPuntos == null || datosPuntos.Count == 0) return;

            int totalSum = datosPuntos.Sum(x => x.Valor);
            if (totalSum == 0) totalSum = 1;

            Rectangle rect = new Rectangle(20, 20, 250, 250);
            float startAngle = 0;

            for (int i = 0; i < datosPuntos.Count; i++)
            {
                float sweepAngle = (float)datosPuntos[i].Valor / totalSum * 360f;
                using Brush b = new SolidBrush(paletaColores[i % paletaColores.Length]);
                g.FillPie(b, rect, startAngle, sweepAngle);
                startAngle += sweepAngle;
            }

            Rectangle hole = new Rectangle(80, 80, 130, 130);
            using (Brush b = new SolidBrush(this.BackColor)) g.FillEllipse(b, hole);

            int yLeg = 20;
            for (int i = 0; i < datosPuntos.Count; i++)
            {
                using (Brush b = new SolidBrush(paletaColores[i % paletaColores.Length])) g.FillRectangle(b, 290, yLeg, 15, 15);
                g.DrawString($"{datosPuntos[i].Etiqueta} ({datosPuntos[i].Valor})", this.Font, Brushes.Black, 310, yLeg);
                yLeg += 25;
            }
        }
    }

    public class PanelGraficoPieMonedas : Panel
    {
        private List<(string Moneda, decimal MontoUSD)> datosPuntos = new List<(string, decimal)>();
        private Color[] paletaColores = new Color[] { Color.MediumSeaGreen, Color.RoyalBlue, Color.DarkOrange, Color.DarkOrchid, Color.Crimson };

        public PanelGraficoPieMonedas() { this.DoubleBuffered = true; }

        public void CargarDatosPieMonedas(List<(string, decimal)> datos)
        {
            datosPuntos = datos;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (datosPuntos == null || datosPuntos.Count == 0) return;

            decimal totalSum = datosPuntos.Sum(x => x.MontoUSD);
            if (totalSum == 0) totalSum = 1m;

            Rectangle rect = new Rectangle(20, 20, 250, 250);
            float startAngle = 0;

            for (int i = 0; i < datosPuntos.Count; i++)
            {
                float sweepAngle = (float)(datosPuntos[i].MontoUSD / totalSum) * 360f;
                using Brush b = new SolidBrush(paletaColores[i % paletaColores.Length]);
                g.FillPie(b, rect, startAngle, sweepAngle);
                startAngle += sweepAngle;
            }

            int yLeg = 20;
            for (int i = 0; i < datosPuntos.Count; i++)
            {
                using (Brush b = new SolidBrush(paletaColores[i % paletaColores.Length])) g.FillRectangle(b, 290, yLeg, 15, 15);
                double pct = (double)(datosPuntos[i].MontoUSD / totalSum) * 100.0;
                g.DrawString($"{datosPuntos[i].Moneda}: $ {datosPuntos[i].MontoUSD:N2} ({pct:F1}%)", this.Font, Brushes.Black, 310, yLeg);
                yLeg += 25;
            }
        }
    }
}