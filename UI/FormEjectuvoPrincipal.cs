using SistemaBancario.Models;
using SistemaBancario.Persistence;
using SistemaBancario.Reporting;
using SistemaBancario.Security;
using SistemaBancario.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SistemaBancario.UI
{
    public partial class FormEjecutivoPrincipal : Form
    {
        private Label lblSaludoBienvenida = null!;
        private Label lblRelojHeader = null!;

        // Navegación
        private Button btnMenuAprobacion = null!;
        private Button btnMenuObservadas = null!;
        private Button btnMenuAuditoria = null!;
        private Button btnMenuSalir = null!;

        // Paneles
        private Panel pnlVistaAprobacion = null!;
        private Panel pnlVistaObservadas = null!;
        private Panel pnlVistaAuditoria = null!;

        // Vista Aprobación
        private DataGridView dgvTransferenciasObservadas = null!;
        private CheckedListBox chkListMotivosRechazo = null!;
        private TextBox txtMotivoRechazo = null!;
        private Button btnAprobarTransferencia = null!;
        private Button btnRechazarTransferencia = null!;

        // Vista Registro de Observadas
        private DataGridView dgvHistorialObservadas = null!;

        // Vista Auditoría
        private ComboBox cmbTipoReporte = null!;
        private ComboBox cmbOpcionReporte = null!;
        private DateTimePicker dtpFechaInicio = null!;
        private DateTimePicker dtpFechaFin = null!;
        private Button btnConsultarAuditoria = null!;
        private Button btnExportarAuditoria = null!;
        private DataGridView dgvAuditoriaEjecutivo = null!;

        private System.Windows.Forms.Timer timerRelojSistema = null!;
        private ResultadoReporteOperacionActual? ultimoReporteAuditoria;

        public FormEjecutivoPrincipal()
        {
            InitializeComponentConfigurado();
            CargarMotivosRechazoEnum();
            InicializarReloj();
            CargarTransferenciasObservadas();
            CargarHistorialObservadas();
            EjecutarConsultaAuditoria();
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

        private void CargarMotivosRechazoEnum()
        {
            chkListMotivosRechazo.Items.Clear();
            foreach (MotivoRechazoBancario motivo in Enum.GetValues(typeof(MotivoRechazoBancario)))
            {
                chkListMotivosRechazo.Items.Add(motivo.ObtenerDescripcion(), false);
            }
        }

        private void InitializeComponentConfigurado()
        {
            this.Text = "Sistema Bancario - Panel del Ejecutivo de Operaciones";
            this.Size = new Size(1720, 840);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);

            string usuario = AuthManager.UsuarioActual?.Username ?? "Ejecutivo_Demo";

            Panel pnlHeader = new Panel()
            {
                Size = new Size(1680, 65),
                Location = new Point(12, 12),
                BackColor = Color.FromArgb(24, 43, 73)
            };

            lblSaludoBienvenida = new Label()
            {
                Text = $"Consola Ejecutiva de Autorizaciones | Usuario: {usuario} | Rol: Executive",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(15, 20),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSaludoBienvenida);

            lblRelojHeader = new Label()
            {
                Text = "Reloj Sistema...",
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(1280, 22),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblRelojHeader);
            this.Controls.Add(pnlHeader);

            Panel pnlNavegacion = new Panel()
            {
                Size = new Size(1680, 50),
                Location = new Point(12, 85),
                BackColor = Color.Gainsboro
            };

            int anchoBtn = 330;
            int altoBtn = 38;

            btnMenuAprobacion = new Button() { Text = "1. 🔍 Solicitudes Pendientes (Revisión)", Size = new Size(anchoBtn, altoBtn), Location = new Point(12, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuAprobacion.Click += (s, e) => MostrarVista("APROBACION");

            btnMenuObservadas = new Button() { Text = "2. 📋 Histórico de Operaciones Observadas", Size = new Size(anchoBtn, altoBtn), Location = new Point(355, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuObservadas.Click += (s, e) => MostrarVista("OBSERVADAS");

            btnMenuAuditoria = new Button() { Text = "3. 📊 Auditoría y Monitoreo General", Size = new Size(anchoBtn, altoBtn), Location = new Point(700, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuAuditoria.Click += (s, e) => MostrarVista("AUDITORIA");

            btnMenuSalir = new Button() { Text = "4. 🚪 Salir", Size = new Size(200, altoBtn), Location = new Point(1460, 6), BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuSalir.Click += BtnMenuSalir_Click;

            pnlNavegacion.Controls.Add(btnMenuAprobacion);
            pnlNavegacion.Controls.Add(btnMenuObservadas);
            pnlNavegacion.Controls.Add(btnMenuAuditoria);
            pnlNavegacion.Controls.Add(btnMenuSalir);
            this.Controls.Add(pnlNavegacion);

            ConstruirVistaAprobacion();
            ConstruirVistaObservadas();
            ConstruirVistaAuditoria();

            MostrarVista("APROBACION");
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
            pnlVistaAprobacion.Visible = (vista == "APROBACION");
            pnlVistaObservadas.Visible = (vista == "OBSERVADAS");
            pnlVistaAuditoria.Visible = (vista == "AUDITORIA");

            if (vista == "APROBACION") CargarTransferenciasObservadas();
            if (vista == "OBSERVADAS") CargarHistorialObservadas();
            if (vista == "AUDITORIA") EjecutarConsultaAuditoria();
        }

        private void ConstruirVistaAprobacion()
        {
            pnlVistaAprobacion = new Panel() { Size = new Size(1680, 640), Location = new Point(12, 142) };

            GroupBox gpxTabla = new GroupBox()
            {
                Text = "Solicitudes Pendientes de Decisión Manual (Cola 2 / UnderReview)",
                Location = new Point(5, 5),
                Size = new Size(1210, 625),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            dgvTransferenciasObservadas = new DataGridView()
            {
                Location = new Point(15, 30),
                Size = new Size(1180, 575),
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            ConfigurarFilaHeadersNumerados(dgvTransferenciasObservadas);
            gpxTabla.Controls.Add(dgvTransferenciasObservadas);
            pnlVistaAprobacion.Controls.Add(gpxTabla);

            GroupBox gpxAcciones = new GroupBox()
            {
                Text = "Acciones de Evaluación",
                Location = new Point(1225, 5),
                Size = new Size(450, 625),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            int yPos = 30;
            gpxAcciones.Controls.Add(new Label() { Text = "Seleccione los Motivos de Rechazo (☑):", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });

            yPos += 22;
            chkListMotivosRechazo = new CheckedListBox()
            {
                Location = new Point(20, yPos),
                Size = new Size(410, 170),
                CheckOnClick = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };
            gpxAcciones.Controls.Add(chkListMotivosRechazo);

            yPos += 180;
            gpxAcciones.Controls.Add(new Label() { Text = "Sustento / Detalle Adicional (Opcional):", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });

            yPos += 22;
            txtMotivoRechazo = new TextBox()
            {
                Location = new Point(20, yPos),
                Size = new Size(410, 90),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            gpxAcciones.Controls.Add(txtMotivoRechazo);

            yPos += 105;
            btnAprobarTransferencia = new Button()
            {
                Text = "✅ Aprobar Solicitud (Pasar a Processed)",
                Location = new Point(20, yPos),
                Size = new Size(410, 55),
                BackColor = Color.LightGreen,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnAprobarTransferencia.Click += BtnAprobarTransferencia_Click;
            gpxAcciones.Controls.Add(btnAprobarTransferencia);

            yPos += 65;
            btnRechazarTransferencia = new Button()
            {
                Text = "❌ Rechazar Solicitud (Marcar Rejected)",
                Location = new Point(20, yPos),
                Size = new Size(410, 55),
                BackColor = Color.LightCoral,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnRechazarTransferencia.Click += BtnRechazarTransferencia_Click;
            gpxAcciones.Controls.Add(btnRechazarTransferencia);

            pnlVistaAprobacion.Controls.Add(gpxAcciones);
            this.Controls.Add(pnlVistaAprobacion);
        }

        private void CargarTransferenciasObservadas()
        {
            try
            {
                DateTime fechaHoy = ConfiguracionOperativa.ObtenerFechaHoraActual();
                var listaObservadas = ArchivoManager.CargarOperacionesObservadas(fechaHoy);

                // FILTRADO Y ORDENAMIENTO POR PRIORIDAD ASIGNADA (8 -> 6 -> 4 -> 2) Y DESEMPATE POR ORDEN DE LLEGADA
                dgvTransferenciasObservadas.DataSource = listaObservadas
                    .Where(o => o.EstadoDecision == "Pendiente")
                    .OrderByDescending(o => o.Transaction.PrioridadAsignada)
                    .ThenBy(o => o.TimestampObservada)
                    .Select(o => new
                    {
                        ID = o.Transaction.TransactionId,
                        FechaHora = o.TimestampObservada.ToString("yyyy-MM-dd HH:mm:ss"),
                        TitularOrigen = o.Transaction.SourceHolderName,
                        CuentaOrigen = o.Transaction.SourceAccountNumber,
                        Monto = $"{o.Transaction.Amount:N2} {o.Transaction.Currency}",
                        CuentaDestino = o.Transaction.DestinationAccountNumber,
                        MotivoObservacion = o.MotivoObservacion,
                        Prioridad = o.Transaction.PrioridadAsignada,
                        Decision = o.EstadoDecision,
                        EstadoActual = o.Transaction.Status
                    }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar solicitudes observadas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAprobarTransferencia_Click(object? sender, EventArgs e)
        {
            if (dgvTransferenciasObservadas.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una transferencia de la lista para aprobar.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string txId = dgvTransferenciasObservadas.SelectedRows[0].Cells["ID"].Value?.ToString() ?? string.Empty;
            DateTime fechaHoy = ConfiguracionOperativa.ObtenerFechaHoraActual();
            string usuario = AuthManager.UsuarioActual?.Username ?? "Ejecutivo_Demo";

            if (ArchivoManager.ActualizarEstadoOperacionObservada(txId, "Processed", fechaHoy, $"Aprobado por ejecutivo [{usuario}]"))
            {
                MessageBox.Show($"La transferencia [{txId}] fue aprobada con éxito y pasó a estado Processed.", "Aprobación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtMotivoRechazo.Clear();
                LimpiarCasillasMotivos();
                CargarTransferenciasObservadas();
            }
            else
            {
                MessageBox.Show("No se pudo actualizar el estado de la transferencia.", "Error de Actualización", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRechazarTransferencia_Click(object? sender, EventArgs e)
        {
            if (dgvTransferenciasObservadas.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una transferencia de la lista para rechazar.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var motivosSeleccionados = new List<string>();
            foreach (var item in chkListMotivosRechazo.CheckedItems)
            {
                motivosSeleccionados.Add(item.ToString() ?? string.Empty);
            }

            if (motivosSeleccionados.Count == 0)
            {
                MessageBox.Show("Debe marcar al menos una casilla (☑) de motivo de rechazo.", "Selección Requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string motivosUnidos = string.Join(" | ", motivosSeleccionados);
            string detalleAdicional = txtMotivoRechazo.Text.Trim();

            string sustentoFinal = string.IsNullOrEmpty(detalleAdicional)
                ? motivosUnidos
                : $"{motivosUnidos} | Obs: {detalleAdicional}";

            string txId = dgvTransferenciasObservadas.SelectedRows[0].Cells["ID"].Value?.ToString() ?? string.Empty;
            DateTime fechaHoy = ConfiguracionOperativa.ObtenerFechaHoraActual();

            if (ArchivoManager.ActualizarEstadoOperacionObservada(txId, "Rejected", fechaHoy, sustentoFinal))
            {
                MessageBox.Show($"La transferencia [{txId}] fue rechazada bajo los siguientes motivos:\n\n{sustentoFinal}", "Rechazo Registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtMotivoRechazo.Clear();
                LimpiarCasillasMotivos();
                CargarTransferenciasObservadas();
            }
            else
            {
                MessageBox.Show("No se pudo actualizar el estado de la transferencia.", "Error de Actualización", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarCasillasMotivos()
        {
            for (int i = 0; i < chkListMotivosRechazo.Items.Count; i++)
            {
                chkListMotivosRechazo.SetItemChecked(i, false);
            }
        }

        private void ConstruirVistaObservadas()
        {
            pnlVistaObservadas = new Panel() { Size = new Size(1680, 640), Location = new Point(12, 142) };

            GroupBox gpxHistorial = new GroupBox()
            {
                Text = "Bitácora Histórica de Evaluaciones Manuales (Observadas)",
                Location = new Point(5, 5),
                Size = new Size(1670, 625),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            dgvHistorialObservadas = new DataGridView()
            {
                Location = new Point(15, 30),
                Size = new Size(1640, 575),
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            ConfigurarFilaHeadersNumerados(dgvHistorialObservadas);
            gpxHistorial.Controls.Add(dgvHistorialObservadas);

            pnlVistaObservadas.Controls.Add(gpxHistorial);
            this.Controls.Add(pnlVistaObservadas);
        }

        private void CargarHistorialObservadas()
        {
            try
            {
                DateTime fechaHoy = ConfiguracionOperativa.ObtenerFechaHoraActual();
                var historial = ArchivoManager.CargarOperacionesObservadas(fechaHoy);

                dgvHistorialObservadas.DataSource = historial.Select(h => new
                {
                    ID = h.Transaction.TransactionId,
                    FechaObservada = h.TimestampObservada.ToString("yyyy-MM-dd HH:mm:ss"),
                    Titular = h.Transaction.SourceHolderName,
                    CuentaOrigen = h.Transaction.SourceAccountNumber,
                    MontoOriginal = $"{h.Transaction.Amount:N2} {h.Transaction.Currency}",
                    CuentaDestino = h.Transaction.DestinationAccountNumber,
                    MotivoEvaluacion = h.MotivoObservacion,
                    Prioridad = h.Transaction.PrioridadAsignada,
                    DecisionEjecutivo = h.EstadoDecision,
                    EstadoFinal = h.Transaction.Status
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConstruirVistaAuditoria()
        {
            pnlVistaAuditoria = new Panel() { Size = new Size(1680, 640), Location = new Point(12, 142) };
            GroupBox gpxFiltros = new GroupBox() { Text = "Auditoría de Transacciones para Ejecutivos", Location = new Point(5, 5), Size = new Size(1670, 625), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            int xPos = 20;
            gpxFiltros.Controls.Add(new Label() { Text = "Tipo Reporte:", Location = new Point(xPos, 30), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });
            cmbTipoReporte = new ComboBox() { Location = new Point(xPos, 55), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipoReporte.Items.AddRange(new object[] { "Operación Actual (Tiempo Real)", "Acumulado Rango de Fechas" });
            cmbTipoReporte.SelectedIndex = 1;
            cmbTipoReporte.SelectedIndexChanged += (s, e) => dtpFechaFin.Enabled = (cmbTipoReporte.SelectedIndex == 1);
            gpxFiltros.Controls.Add(cmbTipoReporte);

            xPos += 265;
            gpxFiltros.Controls.Add(new Label() { Text = "Opciones Contenido:", Location = new Point(xPos, 30), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });
            cmbOpcionReporte = new ComboBox() { Location = new Point(xPos, 55), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbOpcionReporte.Items.AddRange(new object[] { "Solo Detalle Transacciones", "Solo Indicadores (KPIs)", "Ambos (Detalle + KPIs)" });
            cmbOpcionReporte.SelectedIndex = 2;
            gpxFiltros.Controls.Add(cmbOpcionReporte);

            DateTime fechaHoy = ConfiguracionOperativa.ObtenerFechaHoraActual();

            xPos += 265;
            gpxFiltros.Controls.Add(new Label() { Text = "Fecha Inicio:", Location = new Point(xPos, 30), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });
            dtpFechaInicio = new DateTimePicker() { Location = new Point(xPos, 55), Size = new Size(150, 25), Format = DateTimePickerFormat.Short };
            dtpFechaInicio.Value = fechaHoy.Date;
            gpxFiltros.Controls.Add(dtpFechaInicio);

            xPos += 165;
            gpxFiltros.Controls.Add(new Label() { Text = "Fecha Fin:", Location = new Point(xPos, 30), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });
            dtpFechaFin = new DateTimePicker() { Location = new Point(xPos, 55), Size = new Size(150, 25), Format = DateTimePickerFormat.Short };
            dtpFechaFin.Value = fechaHoy.Date;
            gpxFiltros.Controls.Add(dtpFechaFin);

            xPos += 180;
            btnConsultarAuditoria = new Button() { Text = "🔍 Consultar", Location = new Point(xPos, 53), Size = new Size(180, 30), BackColor = Color.LightSkyBlue, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnConsultarAuditoria.Click += (s, e) => EjecutarConsultaAuditoria();
            gpxFiltros.Controls.Add(btnConsultarAuditoria);

            dgvAuditoriaEjecutivo = new DataGridView() { Location = new Point(20, 95), Size = new Size(1630, 465), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgvAuditoriaEjecutivo);
            gpxFiltros.Controls.Add(dgvAuditoriaEjecutivo);

            btnExportarAuditoria = new Button() { Text = "📊 Exportar Reporte de Auditoría (CSV)", Location = new Point(20, 570), Size = new Size(1630, 40), BackColor = Color.Gainsboro, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnExportarAuditoria.Click += BtnExportarAuditoria_Click;
            gpxFiltros.Controls.Add(btnExportarAuditoria);

            pnlVistaAuditoria.Controls.Add(gpxFiltros);
            this.Controls.Add(pnlVistaAuditoria);
        }

        private void EjecutarConsultaAuditoria()
        {
            try
            {
                OpcionReporteOperacionActual opcion = (OpcionReporteOperacionActual)(cmbOpcionReporte.SelectedIndex + 1);

                if (cmbTipoReporte.SelectedIndex == 0)
                {
                    ultimoReporteAuditoria = ReporteManager.ObtenerReporteOperacionActual(dtpFechaInicio.Value, opcion);
                }
                else
                {
                    ultimoReporteAuditoria = ReporteManagerAcumulado.ObtenerReporteRangoFechas(dtpFechaInicio.Value, dtpFechaFin.Value, opcion);
                }

                if (ultimoReporteAuditoria.DetalleTransacciones != null)
                {
                    dgvAuditoriaEjecutivo.DataSource = ultimoReporteAuditoria.DetalleTransacciones.Select(t => new
                    {
                        ID = t.TransactionId,
                        FechaEmision = t.HoraIngreso.ToString("yyyy-MM-dd HH:mm:ss"),
                        SwiftEmision = t.SwiftEmisionInternaTimestamp.HasValue ? t.SwiftEmisionInternaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Pendiente",
                        SwiftTransmision = t.SwiftTransmisionEfectivaTimestamp.HasValue ? t.SwiftTransmisionEfectivaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Programada",
                        Origen = t.SourceAccountNumber,
                        Titular = t.SourceHolderName,
                        MontoOriginal = $"{t.Amount:N2} {t.Currency}",
                        MontoUSD = $"$ {t.AmountInUSD:N2}",
                        Destino = t.DestinationAccountNumber,
                        Prioridad = t.PrioridadAsignada,
                        EstadoProceso = t.StatusActual,
                        EstadoSWIFT = t.EstadoTransmision,
                        Excepcion = t.HasExceptionFlag ? "SÍ" : "NO"
                    }).ToList();
                }
                else
                {
                    dgvAuditoriaEjecutivo.DataSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar reporte de auditoría: {ex.Message}", "Error de Consulta", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportarAuditoria_Click(object? sender, EventArgs e)
        {
            if (ultimoReporteAuditoria?.DetalleTransacciones == null)
            {
                MessageBox.Show("Primero debe ejecutar una consulta válida.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using SaveFileDialog sfd = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"ReporteEjecutivo_{ConfiguracionOperativa.ObtenerFechaHoraActual():yyyyMMdd_HHmm}.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                    sw.WriteLine($"Reporte: {ultimoReporteAuditoria.TipoReporte}");
                    sw.WriteLine($"Fecha Consulta: {ultimoReporteAuditoria.FechaConsultaUtc:yyyy-MM-dd HH:mm:ss}\n");
                    sw.WriteLine("TransactionId;FechaEmision;SwiftEmision;SwiftTransmision;SourceHolder;SourceAccount;Amount;Currency;AmountInUSD;DestinationBank;DestinationAccount;Prioridad;StatusProceso;EstadoSWIFT;HasException");
                    foreach (var t in ultimoReporteAuditoria.DetalleTransacciones)
                    {
                        string emisionStr = t.SwiftEmisionInternaTimestamp.HasValue ? t.SwiftEmisionInternaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                        string transmisionStr = t.SwiftTransmisionEfectivaTimestamp.HasValue ? t.SwiftTransmisionEfectivaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";

                        sw.WriteLine($"{t.TransactionId};{t.HoraIngreso:yyyy-MM-dd HH:mm:ss};{emisionStr};{transmisionStr};\"{t.SourceHolderName}\";{t.SourceAccountNumber};{t.Amount};{t.Currency};{t.AmountInUSD};{t.DestinationBankName};{t.DestinationAccountNumber};{t.PrioridadAsignada};{t.StatusActual};{t.EstadoTransmision};{t.HasExceptionFlag}");
                    }
                    MessageBox.Show("Reporte exportado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnMenuSalir_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("¿Desea salir del Panel Ejecutivo?", "Salir", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                AuthManager.UsuarioActual = null;
                this.Close();
            }
        }
    }
}