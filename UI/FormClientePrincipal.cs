using SistemaBancario.Models;
using SistemaBancario.Persistence;
using SistemaBancario.Reporting;
using SistemaBancario.Security;
using SistemaBancario.Services;
using System.Security.Cryptography;
using System.Text;


namespace SistemaBancario.UI
{
    public class CuentaCliente
    {
        public string NumeroCuenta { get; set; } = string.Empty;
        public string Moneda { get; set; } = "USD";
        public string Estado { get; set; } = "Activa";
        public override string ToString() => $"{NumeroCuenta} ({Moneda})";
    }

    public partial class FormClientePrincipal : Form
    {
        private Label lblSaludoBienvenida = null!;
        private Label lblRelojHeader = null!;
        private Button btnMenuNuevaTransaccion = null!;
        private Button btnMenuGestionCuentas = null!;
        private Button btnMenuExcepciones = null!;
        private Button btnMenuEditarPerfil = null!;
        private Button btnMenuSalir = null!;

        private Panel pnlVistaSolicitud = null!;
        private Panel pnlVistaExcepciones = null!;

        private ComboBox cmbCuentaOrigen = null!;
        private TextBox txtNombreTitular = null!;
        private TextBox txtEmpresaTitular = null!;
        private TextBox txtMonto = null!;
        private ComboBox cmbMoneda = null!;
        private TextBox txtCuentaDestino = null!;
        private TextBox txtNombreBeneficiario = null!;
        private TextBox txtBancoDestino = null!;
        private Button btnEnviarTransferencia = null!;

        private System.Windows.Forms.Timer timerRelojSistema = null!;

        private ComboBox cmbModoConsulta = null!;
        private DateTimePicker dtpFechaInicio = null!;
        private DateTimePicker dtpFechaFin = null!;
        private Button btnConsultar = null!;
        private Button btnExportarReporte = null!;
        private DataGridView dgvHistorialCliente = null!;

        private ComboBox cmbFiltroAckEstado = null!;
        private ComboBox cmbFiltroAckMes = null!;
        private ComboBox cmbFiltroAckAnio = null!;
        private Button btnFiltrarExcepciones = null!;
        private DataGridView dgvExcepcionesCliente = null!;
        private Button btnAcknowledge = null!;

        private List<CuentaCliente> misCuentasRegistradas = new List<CuentaCliente>();
        private GestorTransacciones gestorTransacciones;
        private string usuarioActualKey => AuthManager.UsuarioActual?.Username ?? "cliente_demo";

        public FormClientePrincipal()
        {
            InitializeComponentConfigurado();
            gestorTransacciones = new GestorTransacciones();
            InicializarReloj();

            this.Shown += FormClientePrincipal_Shown;

            CargarCuentasDesdePersistencia();
            CargarDatosCliente();
            ActualizarComboCuentasOrigen();
            EjecutarConsultaHistorial();
            CargarTablaExcepciones();

            EvaluarEstadoBloqueoNavegacion();
        }

        private void FormClientePrincipal_Shown(object? sender, EventArgs e)
        {
            VerificarYForzarActualizacionPerfilPrimerInicio();
        }

        private void EvaluarEstadoBloqueoNavegacion()
        {
            var user = AuthManager.UsuarioActual;
            bool debeBloquear = user != null && user.MustChangePassword;

            if (btnMenuNuevaTransaccion != null) btnMenuNuevaTransaccion.Enabled = !debeBloquear;
            if (btnMenuGestionCuentas != null) btnMenuGestionCuentas.Enabled = !debeBloquear;
            if (btnMenuExcepciones != null) btnMenuExcepciones.Enabled = !debeBloquear;

            if (btnMenuEditarPerfil != null) btnMenuEditarPerfil.Enabled = true;
            if (btnMenuSalir != null) btnMenuSalir.Enabled = true;

            if (pnlVistaSolicitud != null) pnlVistaSolicitud.Enabled = !debeBloquear;
            if (pnlVistaExcepciones != null) pnlVistaExcepciones.Enabled = !debeBloquear;
        }

        private void VerificarYForzarActualizacionPerfilPrimerInicio()
        {
            var user = AuthManager.UsuarioActual;
            if (user != null && user.MustChangePassword)
            {
                MessageBox.Show(
                    $"¡Bienvenido(a) {user.FullName}!\n\nPor políticas de seguridad, en su primer inicio de sesión es obligatorio actualizar su información de perfil y establecer una nueva contraseña.",
                    "Primer Inicio de Sesión Requerido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                MostrarModalActualizacionPerfil(esPrimerInicioObligatorio: true);
            }
        }

        private static string GenerarHashSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void MostrarModalActualizacionPerfil(bool esPrimerInicioObligatorio)
        {
            var user = AuthManager.UsuarioActual;
            if (user == null) return;

            Form formPerfil = new Form
            {
                Text = "Actualización de Perfil de Usuario y Seguridad",
                Size = new Size(580, 640),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = !esPrimerInicioObligatorio
            };

            GroupBox gpxPerfil = new GroupBox
            {
                Text = "Información Personal del Usuario",
                Location = new Point(15, 10),
                Size = new Size(535, 230),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            int y = 30;
            gpxPerfil.Controls.Add(new Label { Text = "Nombre de Usuario:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            TextBox txtUsername = new TextBox { Text = user.Username, Location = new Point(180, y - 3), Size = new Size(330, 23), ReadOnly = true, BackColor = Color.LightGray, Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxPerfil.Controls.Add(txtUsername);

            y += 35;
            gpxPerfil.Controls.Add(new Label { Text = "Rol Asignado:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            TextBox txtRol = new TextBox { Text = user.Role.ToString(), Location = new Point(180, y - 3), Size = new Size(330, 23), Enabled = false, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            gpxPerfil.Controls.Add(txtRol);

            y += 35;
            gpxPerfil.Controls.Add(new Label { Text = "Nombre Completo / Titular:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            TextBox txtNombreCompleto = new TextBox { Text = user.FullName, Location = new Point(180, y - 3), Size = new Size(330, 23), Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxPerfil.Controls.Add(txtNombreCompleto);

            y += 35;
            gpxPerfil.Controls.Add(new Label { Text = "Compañía / Empresa:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            TextBox txtEmpresaPerfil = new TextBox { Text = user.SourceHolderName ?? string.Empty, Location = new Point(180, y - 3), Size = new Size(330, 23), ReadOnly = true, BackColor = Color.LightGray, Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxPerfil.Controls.Add(txtEmpresaPerfil);

            y += 35;
            gpxPerfil.Controls.Add(new Label { Text = "Correo Electrónico:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            TextBox txtEmail = new TextBox { Text = user.Email, Location = new Point(180, y - 3), Size = new Size(330, 23), Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxPerfil.Controls.Add(txtEmail);

            formPerfil.Controls.Add(gpxPerfil);

            GroupBox gpxSeguridad = new GroupBox
            {
                Text = "Actualización Obligatoria de Contraseña",
                Location = new Point(15, 250),
                Size = new Size(535, 230),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            y = 30;
            gpxSeguridad.Controls.Add(new Label { Text = "Nueva Contraseña:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            TextBox txtNuevaPass = new TextBox { Location = new Point(180, y - 3), Size = new Size(330, 23), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxSeguridad.Controls.Add(txtNuevaPass);

            y += 35;
            gpxSeguridad.Controls.Add(new Label { Text = "Confirmar Contraseña:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            TextBox txtConfirmarPass = new TextBox { Location = new Point(180, y - 3), Size = new Size(330, 23), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxSeguridad.Controls.Add(txtConfirmarPass);

            y += 40;
            Panel pnlReglas = new Panel { Location = new Point(20, y), Size = new Size(490, 100), BackColor = Color.FromArgb(235, 243, 250), BorderStyle = BorderStyle.FixedSingle };
            Label lblReglasText = new Label
            {
                Text = "🔒 Requisitos de la Contraseña:\n• Mínimo 8 caracteres\n• Al menos una letra mayúscula (A-Z) y una minúscula (a-z)\n• Al menos un número (0-9) y un carácter especial (@$!%*?&#)",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.DarkSlateBlue,
                Location = new Point(10, 8),
                AutoSize = true
            };
            pnlReglas.Controls.Add(lblReglasText);
            gpxSeguridad.Controls.Add(pnlReglas);

            formPerfil.Controls.Add(gpxSeguridad);

            Button btnGuardarPerfil = new Button
            {
                Text = "💾 Guardar Cambios y Actualizar Perfil",
                Location = new Point(15, 495),
                Size = new Size(535, 50),
                BackColor = Color.LightGreen,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            btnGuardarPerfil.Click += (s, ev) =>
            {
                string nuevoNombre = txtNombreCompleto.Text.Trim();
                string nuevoEmail = txtEmail.Text.Trim();
                string pass1 = txtNuevaPass.Text.Trim();
                string pass2 = txtConfirmarPass.Text.Trim();

                if (string.IsNullOrEmpty(nuevoNombre) || string.IsNullOrEmpty(nuevoEmail))
                {
                    MessageBox.Show("El Nombre Completo y el Email son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(pass1) || string.IsNullOrEmpty(pass2))
                {
                    MessageBox.Show("Debe ingresar y confirmar su nueva contraseña.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (pass1 != pass2)
                {
                    MessageBox.Show("Las contraseñas ingresadas no coinciden.", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string hashNuevaContrasena = GenerarHashSHA256(pass1);

                user.FullName = nuevoNombre;
                user.Email = nuevoEmail;
                user.PasswordHash = hashNuevaContrasena;
                user.MustChangePassword = false;

                var listaUsuarios = UserFileAdapter.CargarTodos();
                var userEnLista = listaUsuarios.FirstOrDefault(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase));

                if (userEnLista != null)
                {
                    userEnLista.FullName = user.FullName;
                    userEnLista.Email = user.Email;
                    userEnLista.PasswordHash = hashNuevaContrasena;
                    userEnLista.MustChangePassword = false;

                    UserFileAdapter.GuardarTodos(listaUsuarios);
                }

                MessageBox.Show("Su perfil y contraseña han sido actualizados exitosamente. El sistema ha quedado habilitado.", "Actualización Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);

                CargarDatosCliente();
                EvaluarEstadoBloqueoNavegacion();
                formPerfil.DialogResult = DialogResult.OK;
                formPerfil.Close();
            };

            formPerfil.Controls.Add(btnGuardarPerfil);

            if (esPrimerInicioObligatorio)
            {
                formPerfil.FormClosing += (s, ev) =>
                {
                    if (user.MustChangePassword && formPerfil.DialogResult != DialogResult.OK)
                    {
                        MessageBox.Show("No puede continuar en el sistema sin completar la actualización de su perfil en su primer inicio.", "Acceso Restringido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        ev.Cancel = true;
                    }
                };
            }

            formPerfil.ShowDialog();
        }

        private void CargarCuentasDesdePersistencia() => misCuentasRegistradas = ArchivoManager.CargarCuentasCliente(usuarioActualKey);
        private void GuardarCuentasEnPersistencia() => ArchivoManager.GuardarCuentasCliente(usuarioActualKey, misCuentasRegistradas);

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

        private void InitializeComponentConfigurado()
        {
            this.Text = "Sistema Bancario - Módulo de Cliente";
            this.Size = new Size(1690, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);

            Panel pnlHeader = new Panel() { Size = new Size(1650, 65), Location = new Point(12, 12), BackColor = Color.FromArgb(24, 43, 73) };
            lblSaludoBienvenida = new Label() { Text = $"¡Bienvenido(a) {AuthManager.UsuarioActual?.FullName ?? "Cliente"}!", ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(15, 20), AutoSize = true };
            lblRelojHeader = new Label() { Text = "Reloj Sistema...", ForeColor = Color.Yellow, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(1250, 22), AutoSize = true };
            pnlHeader.Controls.Add(lblSaludoBienvenida);
            pnlHeader.Controls.Add(lblRelojHeader);
            this.Controls.Add(pnlHeader);

            Panel pnlNavegacion = new Panel() { Size = new Size(1650, 50), Location = new Point(12, 85), BackColor = Color.Gainsboro };
            btnMenuNuevaTransaccion = new Button() { Text = "1. 📝 Generar Solicitud", Size = new Size(270, 38), Location = new Point(12, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuNuevaTransaccion.Click += (s, e) => MostrarVista("SOLICITUD");

            btnMenuGestionCuentas = new Button() { Text = "2. 🏦 Gestionar Cuentas", Size = new Size(270, 38), Location = new Point(290, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuGestionCuentas.Click += BtnMenuGestionCuentas_Click;

            btnMenuExcepciones = new Button() { Text = "3. ⚠️ Rechazos / Excepciones", Size = new Size(280, 38), Location = new Point(570, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuExcepciones.Click += (s, e) => MostrarVista("EXCEPCIONES");

            btnMenuEditarPerfil = new Button() { Text = "4. 👤 Mi Perfil", Size = new Size(220, 38), Location = new Point(860, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuEditarPerfil.Click += (s, e) => MostrarModalActualizacionPerfil(esPrimerInicioObligatorio: false);

            btnMenuSalir = new Button() { Text = "5. 🚪 Salir", Size = new Size(200, 38), Location = new Point(1435, 6), BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMenuSalir.Click += BtnMenuSalir_Click;

            pnlNavegacion.Controls.Add(btnMenuNuevaTransaccion);
            pnlNavegacion.Controls.Add(btnMenuGestionCuentas);
            pnlNavegacion.Controls.Add(btnMenuExcepciones);
            pnlNavegacion.Controls.Add(btnMenuEditarPerfil);
            pnlNavegacion.Controls.Add(btnMenuSalir);
            this.Controls.Add(pnlNavegacion);

            ConstruirVistaSolicitud();
            ConstruirVistaExcepciones();
            MostrarVista("SOLICITUD");
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
            pnlVistaSolicitud.Visible = (vista == "SOLICITUD");
            pnlVistaExcepciones.Visible = (vista == "EXCEPCIONES");
            if (vista == "EXCEPCIONES") CargarTablaExcepciones();
        }

        private void ConstruirVistaSolicitud()
        {
            pnlVistaSolicitud = new Panel() { Size = new Size(1650, 610), Location = new Point(12, 142) };
            GroupBox gpxTransferencia = new GroupBox() { Text = "Nueva Solicitud de Transferencia", Location = new Point(5, 5), Size = new Size(540, 595), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            int yPos = 30;
            gpxTransferencia.Controls.Add(CrearEtiqueta("Moneda Operación:", 20, yPos));
            cmbMoneda = new ComboBox() { Location = new Point(200, yPos), Size = new Size(310, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMoneda.Items.AddRange(new object[] { "USD", "EUR", "S/." });
            cmbMoneda.SelectedIndex = 0;
            cmbMoneda.SelectedIndexChanged += (s, e) => ActualizarComboCuentasOrigen();
            gpxTransferencia.Controls.Add(cmbMoneda);

            yPos += 45;
            gpxTransferencia.Controls.Add(CrearEtiqueta("Cuenta Origen:", 20, yPos));
            cmbCuentaOrigen = new ComboBox() { Location = new Point(200, yPos), Size = new Size(310, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            gpxTransferencia.Controls.Add(cmbCuentaOrigen);

            yPos += 45;
            gpxTransferencia.Controls.Add(CrearEtiqueta("Usuario Solicitante:", 20, yPos));
            txtNombreTitular = new TextBox() { Location = new Point(200, yPos), Size = new Size(310, 25), ReadOnly = true };
            gpxTransferencia.Controls.Add(txtNombreTitular);

            yPos += 45;
            gpxTransferencia.Controls.Add(CrearEtiqueta("Compañía / Empresa:", 20, yPos));
            txtEmpresaTitular = new TextBox() { Location = new Point(200, yPos), Size = new Size(310, 25), ReadOnly = true };
            gpxTransferencia.Controls.Add(txtEmpresaTitular);

            yPos += 45;
            gpxTransferencia.Controls.Add(CrearEtiqueta("Monto:", 20, yPos));
            txtMonto = new TextBox() { Location = new Point(200, yPos), Size = new Size(310, 25) };
            gpxTransferencia.Controls.Add(txtMonto);

            yPos += 45;
            gpxTransferencia.Controls.Add(CrearEtiqueta("Cuenta Destino:", 20, yPos));
            txtCuentaDestino = new TextBox() { Location = new Point(200, yPos), Size = new Size(310, 25) };
            gpxTransferencia.Controls.Add(txtCuentaDestino);

            yPos += 45;
            gpxTransferencia.Controls.Add(CrearEtiqueta("Beneficiario:", 20, yPos));
            txtNombreBeneficiario = new TextBox() { Location = new Point(200, yPos), Size = new Size(310, 25) };
            gpxTransferencia.Controls.Add(txtNombreBeneficiario);

            yPos += 45;
            gpxTransferencia.Controls.Add(CrearEtiqueta("Banco Destino:", 20, yPos));
            txtBancoDestino = new TextBox() { Location = new Point(200, yPos), Size = new Size(310, 25), Text = "Banco Internacional S.A." };
            gpxTransferencia.Controls.Add(txtBancoDestino);

            yPos += 55;
            btnEnviarTransferencia = new Button() { Text = "Ejecutar Transferencia", Location = new Point(200, yPos), Size = new Size(310, 45), BackColor = Color.LightGreen, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnEnviarTransferencia.Click += BtnEnviarTransferencia_Click;
            gpxTransferencia.Controls.Add(btnEnviarTransferencia);

            pnlVistaSolicitud.Controls.Add(gpxTransferencia);

            GroupBox gpxHistorial = new GroupBox() { Text = "Consulta de Estado de Transacciones", Location = new Point(555, 5), Size = new Size(1085, 595), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            gpxHistorial.Controls.Add(new Label() { Text = "Modo:", Location = new Point(20, 30), AutoSize = true });
            cmbModoConsulta = new ComboBox() { Location = new Point(20, 55), Size = new Size(220, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbModoConsulta.Items.AddRange(new object[] { "Tiempo Real (En Proceso)", "Acumulado Rango" });
            cmbModoConsulta.SelectedIndex = 0;
            cmbModoConsulta.SelectedIndexChanged += (s, e) =>
            {
                bool esRango = cmbModoConsulta.SelectedIndex == 1;
                dtpFechaInicio.Enabled = esRango;
                dtpFechaFin.Enabled = esRango;
            };
            gpxHistorial.Controls.Add(cmbModoConsulta);

            dtpFechaInicio = new DateTimePicker() { Location = new Point(260, 55), Size = new Size(180, 25), Format = DateTimePickerFormat.Short, Enabled = false };
            dtpFechaFin = new DateTimePicker() { Location = new Point(460, 55), Size = new Size(180, 25), Format = DateTimePickerFormat.Short, Enabled = false };
            gpxHistorial.Controls.Add(dtpFechaInicio);
            gpxHistorial.Controls.Add(dtpFechaFin);

            btnConsultar = new Button() { Text = "🔍 Buscar", Location = new Point(660, 53), Size = new Size(180, 30), BackColor = Color.LightSkyBlue };
            btnConsultar.Click += (s, e) => EjecutarConsultaHistorial();
            gpxHistorial.Controls.Add(btnConsultar);

            dgvHistorialCliente = new DataGridView() { Location = new Point(20, 95), Size = new Size(1045, 435), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgvHistorialCliente);
            gpxHistorial.Controls.Add(dgvHistorialCliente);

            btnExportarReporte = new Button() { Text = "📊 Exportar Reporte a Excel / CSV", Location = new Point(20, 540), Size = new Size(1045, 40), BackColor = Color.Gainsboro };
            btnExportarReporte.Click += BtnExportarReporte_Click;
            gpxHistorial.Controls.Add(btnExportarReporte);

            pnlVistaSolicitud.Controls.Add(gpxHistorial);
            this.Controls.Add(pnlVistaSolicitud);
        }

        private void ConstruirVistaExcepciones()
        {
            pnlVistaExcepciones = new Panel() { Size = new Size(1650, 610), Location = new Point(12, 142) };
            GroupBox gpxEx = new GroupBox() { Text = "Notificaciones de Rechazos y Excepciones de Sistema", Location = new Point(5, 5), Size = new Size(1640, 595), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            int xPos = 20;
            gpxEx.Controls.Add(new Label() { Text = "Estado Acknowledge:", Location = new Point(xPos, 30), AutoSize = true });
            cmbFiltroAckEstado = new ComboBox() { Location = new Point(xPos, 55), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltroAckEstado.Items.AddRange(new object[] { "Pendientes", "Confirmados (Acknowledged)", "Todos" });
            cmbFiltroAckEstado.SelectedIndex = 0;
            gpxEx.Controls.Add(cmbFiltroAckEstado);

            xPos += 270;
            gpxEx.Controls.Add(new Label() { Text = "Mes de Consulta:", Location = new Point(xPos, 30), AutoSize = true });
            cmbFiltroAckMes = new ComboBox() { Location = new Point(xPos, 55), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltroAckMes.Items.AddRange(new object[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" });
            cmbFiltroAckMes.SelectedIndex = ConfiguracionOperativa.ObtenerFechaHoraActual().Month - 1;
            gpxEx.Controls.Add(cmbFiltroAckMes);

            xPos += 220;
            gpxEx.Controls.Add(new Label() { Text = "Año:", Location = new Point(xPos, 30), AutoSize = true });
            cmbFiltroAckAnio = new ComboBox() { Location = new Point(xPos, 55), Size = new Size(140, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            int anioActual = ConfiguracionOperativa.ObtenerFechaHoraActual().Year;
            for (int a = anioActual - 2; a <= anioActual + 1; a++) cmbFiltroAckAnio.Items.Add(a.ToString());
            cmbFiltroAckAnio.SelectedItem = anioActual.ToString();
            gpxEx.Controls.Add(cmbFiltroAckAnio);

            xPos += 160;
            btnFiltrarExcepciones = new Button() { Text = "🔍 Filtrar Consulta", Location = new Point(xPos, 53), Size = new Size(200, 28), BackColor = Color.LightSkyBlue };
            btnFiltrarExcepciones.Click += (s, e) => CargarTablaExcepciones();
            gpxEx.Controls.Add(btnFiltrarExcepciones);

            dgvExcepcionesCliente = new DataGridView() { Location = new Point(20, 95), Size = new Size(1600, 420), AllowUserToAddRows = false, ReadOnly = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgvExcepcionesCliente);
            gpxEx.Controls.Add(dgvExcepcionesCliente);

            btnAcknowledge = new Button() { Text = "☑️ Confirmar Lectura / Acknowledge de Seleccionadas", Location = new Point(20, 530), Size = new Size(1600, 45), BackColor = Color.Khaki, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnAcknowledge.Click += BtnAcknowledge_Click;
            gpxEx.Controls.Add(btnAcknowledge);

            pnlVistaExcepciones.Controls.Add(gpxEx);
            this.Controls.Add(pnlVistaExcepciones);
        }

        private Label CrearEtiqueta(string texto, int x, int y) => new Label() { Text = texto, Location = new Point(x, y + 3), AutoSize = true };

        private void CargarDatosCliente()
        {
            var u = AuthManager.UsuarioActual;
            txtNombreTitular.Text = u?.Username ?? "cliente_demo";

            string empresa = u?.SourceHolderName ?? string.Empty;

            if (string.IsNullOrWhiteSpace(empresa) && u is UsuarioCliente cliente)
            {
                empresa = cliente.CompanyName ?? string.Empty;
            }

            txtEmpresaTitular.Text = !string.IsNullOrWhiteSpace(empresa) ? empresa : "Empresa Cliente SAC";
            lblSaludoBienvenida.Text = $"¡Bienvenido(a) {u?.FullName ?? "Cliente"}!";
        }

        private void ActualizarComboCuentasOrigen()
        {
            CargarCuentasDesdePersistencia();
            string monedaSeleccionada = cmbMoneda.SelectedItem?.ToString() ?? "USD";
            var cuentasFiltradas = misCuentasRegistradas.Where(c => c.Moneda == monedaSeleccionada && c.Estado == "Activa").ToList();
            cmbCuentaOrigen.DataSource = null;
            if (cuentasFiltradas.Count > 0)
            {
                cmbCuentaOrigen.DataSource = cuentasFiltradas;
                cmbCuentaOrigen.DisplayMember = "NumeroCuenta";
            }
            else
            {
                cmbCuentaOrigen.Items.Clear();
                cmbCuentaOrigen.Text = string.Empty;
            }
        }

        private void BtnEnviarTransferencia_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtMonto.Text) || !decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
                {
                    MessageBox.Show("Ingrese un monto válido mayor a cero.", "Dato Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string cuentaOrigenSeleccionada = (cmbCuentaOrigen.SelectedItem as CuentaCliente)?.NumeroCuenta ?? cmbCuentaOrigen.Text;
                string monedaSeleccionada = cmbMoneda.SelectedItem?.ToString() ?? "USD";
                DateTime fechaActual = ConfiguracionOperativa.ObtenerFechaHoraActual();

                var tx = new Transaction
                {
                    TransactionId = $"TX-CLI-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                    Amount = monto,
                    Currency = monedaSeleccionada,
                    DestinationCurrency = monedaSeleccionada,
                    SourceAccountNumber = cuentaOrigenSeleccionada,
                    SourceHolderName = txtEmpresaTitular.Text.Trim(),
                    DestinationAccountNumber = txtCuentaDestino.Text.Trim(),
                    DestinationHolderName = txtNombreBeneficiario.Text.Trim(),
                    DestinationBankName = txtBancoDestino.Text.Trim(),
                    DestinationSwiftCode = "SWIFTGLOBAL01",
                    ExecutionTimestamp = fechaActual,
                    Status = "Processed"
                };

                var historialHoy = ArchivoManager.CargarSolicitudesDiarias(fechaActual);
                bool esFrecuente = !GestorReglasNegocio.DebeIrARevisionManual(tx, historialHoy);

                UsuarioCliente? clienteActual = AuthManager.UsuarioActual as UsuarioCliente;
                string resultadoProceso = gestorTransacciones.ProcesarSolicitudTransferencia(tx, clienteActual!, esFrecuente);

                ArchivoManager.RegistrarTransaccionSolicitada(tx, fechaActual);

                if (tx.Status == "UnderReview")
                {
                    ArchivoManager.RegistrarOperacionObservada(tx, "Monto elevado sujeto a revisión manual", fechaActual);
                }

                MessageBox.Show(resultadoProceso, "Resultado de Operación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtMonto.Clear();
                txtCuentaDestino.Clear();
                txtNombreBeneficiario.Clear();
                EjecutarConsultaHistorial();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al procesar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EjecutarConsultaHistorial()
        {
            try
            {
                CargarCuentasDesdePersistencia();
                var setTodasMisCuentas = misCuentasRegistradas.Select(c => c.NumeroCuenta.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                string cuentaSeleccionada = (cmbCuentaOrigen.SelectedItem as CuentaCliente)?.NumeroCuenta?.Trim() ?? cmbCuentaOrigen.Text.Trim();
                string titularEmpresa = txtEmpresaTitular.Text.Trim();

                DateTime fechaActual = ConfiguracionOperativa.ObtenerFechaHoraActual();

                // 1. Cargar las solicitudes registradas directamente del Datalog con la prioridad exacta
                List<Transaction> solicitudes = ArchivoManager.CargarSolicitudesDiarias(fechaActual);

                // 2. Si es consulta por Rango Acumulado, se apoya también en la consulta de reporte
                if (cmbModoConsulta.SelectedIndex == 1)
                {
                    var r = ReporteManagerAcumulado.ObtenerReporteRangoFechas(dtpFechaInicio.Value, dtpFechaFin.Value, OpcionReporteOperacionActual.SoloDetalleTransacciones);
                    var listaReporte = (r.DetalleTransacciones ?? new List<TransaccionOperacionActualDto>())
                        .Where(t =>
                            (!string.IsNullOrEmpty(cuentaSeleccionada) && t.SourceAccountNumber.Trim().Equals(cuentaSeleccionada, StringComparison.OrdinalIgnoreCase)) ||
                            (string.IsNullOrEmpty(cuentaSeleccionada) && (setTodasMisCuentas.Contains(t.SourceAccountNumber.Trim()) || t.SourceHolderName.Trim().Equals(titularEmpresa, StringComparison.OrdinalIgnoreCase)))
                        )
                        .Select(t => new
                        {
                            ID = t.TransactionId,
                            FechaHoraEmisionSolicitud = t.HoraIngreso.ToString("yyyy-MM-dd HH:mm:ss"),
                            MontoOriginal = $"{t.Amount:N2} {t.Currency}",
                            MontoUSD = $"$ {t.AmountInUSD:N2}",
                            Destino = t.DestinationAccountNumber,
                            Prioridad = t.PrioridadAsignada,
                            EstadoProceso = t.StatusActual,
                            EstadoTransmisionSWIFT = string.IsNullOrEmpty(t.EstadoTransmision) ? "Pendiente" : t.EstadoTransmision
                        }).ToList();

                    dgvHistorialCliente.DataSource = null;
                    dgvHistorialCliente.DataSource = listaReporte;
                    return;
                }

                // 3. Consulta en Tiempo Real (En Proceso): Filtra y obtiene la prioridad mediante la coincidencia del TransactionId
                var misTxs = solicitudes.Where(t =>
                    (!string.IsNullOrEmpty(cuentaSeleccionada) && t.SourceAccountNumber.Trim().Equals(cuentaSeleccionada, StringComparison.OrdinalIgnoreCase)) ||
                    (string.IsNullOrEmpty(cuentaSeleccionada) && (setTodasMisCuentas.Contains(t.SourceAccountNumber.Trim()) || t.SourceHolderName.Trim().Equals(titularEmpresa, StringComparison.OrdinalIgnoreCase)))
                )
                .OrderByDescending(t => t.PrioridadAsignada)
                .ThenBy(t => t.ExecutionTimestamp)
                .Select(t => new
                {
                    ID = t.TransactionId,
                    FechaHoraEmisionSolicitud = t.ExecutionTimestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    MontoOriginal = $"{t.Amount:N2} {t.Currency}",
                    MontoUSD = $"$ {t.AmountInUSD:N2}",
                    Destino = t.DestinationAccountNumber,
                    Prioridad = t.PrioridadAsignada, // Obtiene la prioridad calculada (8, 6, 4, 2)
                    EstadoProceso = t.Status,
                    EstadoTransmisionSWIFT = string.IsNullOrEmpty(t.EstadoTransmision) ? "Pendiente" : t.EstadoTransmision
                }).ToList();

                dgvHistorialCliente.DataSource = null;
                dgvHistorialCliente.DataSource = misTxs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar historial: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarTablaExcepciones()
        {
            try
            {
                DateTime fechaSistema = ConfiguracionOperativa.ObtenerFechaHoraActual();
                int mesSeleccionado = (cmbFiltroAckMes?.SelectedIndex ?? (fechaSistema.Month - 1)) + 1;
                int anioSeleccionado = int.Parse(cmbFiltroAckAnio?.SelectedItem?.ToString() ?? fechaSistema.Year.ToString());

                DateTime fechaInicioMes = new DateTime(anioSeleccionado, mesSeleccionado, 1);
                DateTime fechaFinMes = fechaInicioMes.AddMonths(1).AddDays(-1);

                var r = ReporteManagerAcumulado.ObtenerReporteRangoFechas(fechaInicioMes, fechaFinMes, OpcionReporteOperacionActual.SoloDetalleTransacciones);

                CargarCuentasDesdePersistencia();
                var setMisCuentas = misCuentasRegistradas.Select(c => c.NumeroCuenta.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                string estadoFiltro = cmbFiltroAckEstado?.SelectedItem?.ToString() ?? "Pendientes";

                var query = (r.DetalleTransacciones ?? new List<TransaccionOperacionActualDto>())
                    .Where(t => setMisCuentas.Contains(t.SourceAccountNumber.Trim()) && (t.StatusActual == "Rejected" || t.HasExceptionFlag));

                if (estadoFiltro == "Pendientes") query = query.Where(t => !t.Acknowledged);
                else if (estadoFiltro == "Confirmados (Acknowledged)") query = query.Where(t => t.Acknowledged);

                dgvExcepcionesCliente.DataSource = query.Select(t => new NotificacionAcknowledgeDto
                {
                    Seleccionar = false,
                    ID = t.TransactionId,
                    FechaHora = t.HoraIngreso.ToString("yyyy-MM-dd HH:mm"),
                    Monto = $"{t.Amount:N2} {t.Currency}",
                    Estado = t.StatusActual,
                    Observacion = t.HasExceptionFlag ? "Excepción de Sistema" : "Rechazado por Ejecutivo",
                    Acknowledge = t.Acknowledged ? "CONFIRMADO" : "PENDIENTE"
                }).ToList();

                var colSel = dgvExcepcionesCliente.Columns["Seleccionar"];
                if (colSel != null) { colSel.HeaderText = "Confirmar"; colSel.ReadOnly = false; }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar excepciones: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAcknowledge_Click(object? sender, EventArgs e)
        {
            var seleccionadas = new List<string>();
            foreach (DataGridViewRow row in dgvExcepcionesCliente.Rows)
            {
                if (Convert.ToBoolean(row.Cells["Seleccionar"].Value ?? false))
                {
                    string txId = row.Cells["ID"].Value?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(txId)) seleccionadas.Add(txId);
                }
            }

            if (seleccionadas.Count == 0)
            {
                MessageBox.Show("Marque con un Check al menos una transacción para confirmar lectura.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int anio = int.TryParse(cmbFiltroAckAnio?.SelectedItem?.ToString(), out int a) ? a : ConfiguracionOperativa.ObtenerFechaHoraActual().Year;
            int mes = (cmbFiltroAckMes?.SelectedIndex ?? 0) + 1;
            int actualizadas = 0;

            foreach (var txId in seleccionadas)
            {
                if (ArchivoManager.MarcarAcknowledgeTransaccion(txId, "Lectura confirmada por el cliente", anio, mes)) actualizadas++;
            }

            MessageBox.Show($"Se confirmó la lectura de {actualizadas} transacción(es).", "Acknowledge Exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CargarTablaExcepciones();
        }

        public class NotificacionAcknowledgeDto
        {
            public bool Seleccionar { get; set; }
            public string ID { get; set; } = string.Empty;
            public string FechaHora { get; set; } = string.Empty;
            public string Monto { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public string Observacion { get; set; } = string.Empty;
            public string Acknowledge { get; set; } = string.Empty;
        }

        private void BtnExportarReporte_Click(object? sender, EventArgs e)
        {
            using SaveFileDialog sfd = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"Reporte_Cliente_{ConfiguracionOperativa.ObtenerFechaHoraActual():yyyyMMdd_HHmmss}.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using StreamWriter sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8);
                    sw.WriteLine("ID;FechaEmisionSolicitud;MontoOriginal;MontoUSD;Destino;Prioridad;EstadoProceso;EstadoTransmisionSWIFT");
                    for (int i = 0; i < dgvHistorialCliente.Rows.Count; i++)
                    {
                        var r = dgvHistorialCliente.Rows[i];
                        sw.WriteLine($"{r.Cells[0].Value};{r.Cells[1].Value};{r.Cells[2].Value};{r.Cells[3].Value};{r.Cells[4].Value};{r.Cells[5].Value};{r.Cells[6].Value};{r.Cells[7].Value}");
                    }
                    MessageBox.Show("Reporte exportado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnMenuGestionCuentas_Click(object? sender, EventArgs e)
        {
            Form formCuentas = new Form
            {
                Text = "Gestión Integral de Cuentas Bancarias",
                Size = new Size(820, 620),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            GroupBox gpxNueva = new GroupBox
            {
                Text = "Registrar Nueva Cuenta",
                Location = new Point(15, 10),
                Size = new Size(775, 100),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            TextBox txtNuevaCuenta = new TextBox { Location = new Point(110, 32), Size = new Size(250, 23) };
            ComboBox cmbMonedaNueva = new ComboBox { Location = new Point(440, 32), Size = new Size(130, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMonedaNueva.Items.AddRange(new object[] { "S/.", "USD", "EUR" });
            cmbMonedaNueva.SelectedIndex = 0;
            Button btnGuardarCuenta = new Button { Text = "➕ Registrar", Location = new Point(590, 28), Size = new Size(165, 50), BackColor = Color.LightGreen, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

            gpxNueva.Controls.Add(new Label { Text = "N° Cuenta:", Location = new Point(15, 35), AutoSize = true });
            gpxNueva.Controls.Add(txtNuevaCuenta);
            gpxNueva.Controls.Add(new Label { Text = "Moneda:", Location = new Point(375, 35), AutoSize = true });
            gpxNueva.Controls.Add(cmbMonedaNueva);
            gpxNueva.Controls.Add(btnGuardarCuenta);
            formCuentas.Controls.Add(gpxNueva);

            GroupBox gpxLista = new GroupBox
            {
                Text = "Cuentas Registradas del Cliente",
                Location = new Point(15, 120),
                Size = new Size(775, 440),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            DataGridView dgvCuentas = new DataGridView
            {
                Location = new Point(15, 25),
                Size = new Size(745, 330),
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            ConfigurarFilaHeadersNumerados(dgvCuentas);

            Action CargarGrillaCuentas = () =>
            {
                CargarCuentasDesdePersistencia();
                dgvCuentas.DataSource = misCuentasRegistradas.Select(c => new
                {
                    Cuenta = c.NumeroCuenta,
                    Moneda = c.Moneda,
                    Estado = c.Estado
                }).ToList();
            };

            CargarGrillaCuentas();

            btnGuardarCuenta.Click += (s, ev) =>
            {
                string num = txtNuevaCuenta.Text.Trim();
                if (string.IsNullOrWhiteSpace(num))
                {
                    MessageBox.Show("Ingrese un número de cuenta válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (misCuentasRegistradas.Any(c => c.NumeroCuenta.Equals(num, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("La cuenta ingresada ya se encuentra registrada.", "Cuenta Duplicada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                misCuentasRegistradas.Add(new CuentaCliente { NumeroCuenta = num, Moneda = cmbMonedaNueva.SelectedItem?.ToString() ?? "USD", Estado = "Activa" });
                GuardarCuentasEnPersistencia();
                txtNuevaCuenta.Clear();
                CargarGrillaCuentas();
                ActualizarComboCuentasOrigen();
            };

            Button btnToggleEstado = new Button
            {
                Text = "🔄 Activar / Desactivar Cuenta",
                Location = new Point(15, 370),
                Size = new Size(360, 45),
                BackColor = Color.Khaki,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            btnToggleEstado.Click += (s, ev) =>
            {
                if (dgvCuentas.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Seleccione una cuenta de la tabla para cambiar su estado.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string numCuentaSel = dgvCuentas.SelectedRows[0].Cells["Cuenta"].Value?.ToString() ?? string.Empty;
                var cuentaTarget = misCuentasRegistradas.FirstOrDefault(c => c.NumeroCuenta == numCuentaSel);

                if (cuentaTarget != null)
                {
                    cuentaTarget.Estado = cuentaTarget.Estado == "Activa" ? "Inactiva" : "Activa";
                    GuardarCuentasEnPersistencia();
                    CargarGrillaCuentas();
                    ActualizarComboCuentasOrigen();
                    MessageBox.Show($"La cuenta [{numCuentaSel}] cambió su estado a [{cuentaTarget.Estado}].", "Estado Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            Button btnEliminarCuenta = new Button
            {
                Text = "❌ Eliminar Cuenta",
                Location = new Point(390, 370),
                Size = new Size(370, 45),
                BackColor = Color.LightCoral,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            btnEliminarCuenta.Click += (s, ev) =>
            {
                if (dgvCuentas.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Seleccione una cuenta de la tabla para eliminar.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string numCuentaSel = dgvCuentas.SelectedRows[0].Cells["Cuenta"].Value?.ToString() ?? string.Empty;

                if (MessageBox.Show($"¿Está seguro de eliminar la cuenta [{numCuentaSel}]?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    misCuentasRegistradas.RemoveAll(c => c.NumeroCuenta == numCuentaSel);
                    GuardarCuentasEnPersistencia();
                    CargarGrillaCuentas();
                    ActualizarComboCuentasOrigen();
                    MessageBox.Show($"La cuenta [{numCuentaSel}] fue eliminada exitosamente.", "Cuenta Eliminada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            gpxLista.Controls.Add(dgvCuentas);
            gpxLista.Controls.Add(btnToggleEstado);
            gpxLista.Controls.Add(btnEliminarCuenta);
            formCuentas.Controls.Add(gpxLista);

            formCuentas.ShowDialog();
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
}