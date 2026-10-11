using SistemaBancario.Models;
using SistemaBancario.Reporting;
using SistemaBancario.Security;
using SistemaBancario.Services;

namespace SistemaBancario.UI
{
    public partial class FormAdminPrincipal : Form
    {
        private Label lblSaludoBienvenida = null!;
        private Label lblRelojHeader = null!;

        // Botones de Navegación
        private Button btnMenuUsuarios = null!;
        private Button btnMenuParametros = null!;
        private Button btnMenuPruebas = null!;
        private Button btnMenuReiniciar = null!;
        private Button btnMenuReportes = null!;
        private Button btnMenuKpis = null!;
        private Button btnMenuSalir = null!;

        // Paneles Contenedores de Vistas
        private Panel pnlVistaUsuarios = null!;
        private Panel pnlVistaParametros = null!;
        private Panel pnlVistaPruebas = null!;
        private Panel pnlVistaReiniciar = null!;
        private Panel pnlVistaReportes = null!;
        private Panel pnlVistaKpis = null!;

        // Controles Vista Gestión de Usuarios
        private TextBox txtNuevoUsername = null!;
        private TextBox txtNuevoNombreCompleto = null!;
        private TextBox txtNuevoEmpresaTitular = null!;
        private TextBox txtNuevoEmail = null!;
        private TextBox txtNuevaPasswordInicial = null!;
        private ComboBox cmbNuevoRol = null!;
        private CheckBox chkEsClienteCorporativo = null!;
        private Button btnCrearUsuario = null!;
        private Button btnToggleEstadoUsuario = null!;
        private Button btnToggleCorporativoUsuario = null!;
        private Button btnEliminarUsuario = null!;
        private DataGridView dgvListaUsuarios = null!;

        // Controles Vista Parametrización
        private DateTimePicker dtpHoraCorteSwift = null!;
        private DateTimePicker dtpHoraCorteRevision = null!;
        private ComboBox cmbTipoHorarioSistema = null!;
        private Button btnGuardarParametros = null!;

        // Controles Vista Reportes / Auditoría
        private ComboBox cmbTipoReporte = null!;
        private ComboBox cmbOpcionReporte = null!;
        private DateTimePicker dtpFechaInicio = null!;
        private DateTimePicker dtpFechaFin = null!;
        private CheckBox chkUsarCarpetaPruebas = null!;
        private Button btnGenerarReporte = null!;
        private Button btnExportarReporte = null!;
        private DataGridView dgvDetalleReporte = null!;

        // Controles Vista KPIs
        private Label lblKpiTotalSolicitudes = null!;
        private Label lblKpiMontoProcesado = null!;
        private Label lblKpiTasaEfectividad = null!;
        private Label lblKpiPorcentajeExcepciones = null!;
        private DataGridView dgvResumenKpisAdmin = null!;
        private PanelGraficoKpi panelGraficoEstado = null!;
        private PanelGraficoPieMonedas panelGraficoMonedas = null!;

        private System.Windows.Forms.Timer timerRelojSistema = null!;
        private ResultadoReporteOperacionActual? ultimoReporteGenerado;

        public FormAdminPrincipal()
        {
            InitializeComponentConfigurado();
            ConfirmarEntornoDeTrabajo();
            CargarConfiguracionInicial();
            InicializarReloj();
            CargarUsuariosEnGrilla();
            EjecutarConsultaYActualizacion();
        }

        private void ConfirmarEntornoDeTrabajo()
        {
            DialogResult result = MessageBox.Show(
                "¿Desea acceder al sistema en MODO PRUEBAS (Alimentado por la carpeta DataLogsPruebas)?\n\n" +
                "• [Sí]  -> MODO PRUEBAS ACTIVO: Los formularios, consultas y simulaciones consumirán/persistirán en DataLogsPruebas.\n" +
                "• [No]  -> MODO PRODUCCIÓN NORMAL: Los formularios operarán sobre los datos reales de DataLogs.",
                "Selección de Entorno de Trabajo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            ConfiguracionOperativa.ModoPruebasActivo = (result == DialogResult.Yes);

            if (chkUsarCarpetaPruebas != null)
            {
                chkUsarCarpetaPruebas.Checked = ConfiguracionOperativa.ModoPruebasActivo;
            }

            if (ConfiguracionOperativa.ModoPruebasActivo)
            {
                this.Text += " - [ENTORNO DE PRUEBAS - DataLogsPruebas]";
                lblSaludoBienvenida.Text += " | 🧪 MODO PRUEBAS ACTIVO";
                lblSaludoBienvenida.ForeColor = Color.Yellow;
            }
        }

        private void InitializeComponentConfigurado()
        {
            this.Text = "Sistema Bancario - Panel del Administrador";
            this.Size = new Size(1690, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);

            string usuario = AuthManager.UsuarioActual?.Username ?? "Admin_Master";

            Panel pnlHeader = new Panel()
            {
                Size = new Size(1650, 65),
                Location = new Point(12, 12),
                BackColor = Color.FromArgb(24, 43, 73)
            };

            lblSaludoBienvenida = new Label()
            {
                Text = $"Consola Administradora | Usuario: {usuario} | Rol: Administrator",
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
                Location = new Point(1250, 22),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblRelojHeader);
            this.Controls.Add(pnlHeader);

            Panel pnlNavegacion = new Panel()
            {
                Size = new Size(1650, 50),
                Location = new Point(12, 85),
                BackColor = Color.Gainsboro
            };

            int anchoBtn = 220;
            int altoBtn = 38;

            btnMenuUsuarios = new Button() { Text = "1. 👤 Usuarios", Size = new Size(anchoBtn, altoBtn), Location = new Point(10, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuUsuarios.Click += (s, e) => MostrarVista("USUARIOS");

            btnMenuParametros = new Button() { Text = "2. ⚙️ Parametrización", Size = new Size(anchoBtn, altoBtn), Location = new Point(235, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuParametros.Click += (s, e) => MostrarVista("PARAMETROS");

            btnMenuPruebas = new Button() { Text = "3. 🧪 Pruebas Sistema", Size = new Size(anchoBtn, altoBtn), Location = new Point(460, 6), BackColor = Color.Khaki, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuPruebas.Click += BtnMenuPruebas_Click;

            btnMenuReiniciar = new Button() { Text = "4. 🔄 Reiniciar Sistema", Size = new Size(anchoBtn, altoBtn), Location = new Point(685, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuReiniciar.Click += (s, e) => MostrarVista("REINICIAR");

            btnMenuReportes = new Button() { Text = "5. 📊 Auditoría", Size = new Size(anchoBtn, altoBtn), Location = new Point(910, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuReportes.Click += (s, e) => MostrarVista("REPORTES");

            btnMenuKpis = new Button() { Text = "6. 📈 KPIs", Size = new Size(anchoBtn, altoBtn), Location = new Point(1135, 6), BackColor = Color.White, FlatStyle = FlatStyle.Popup, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuKpis.Click += (s, e) => MostrarVista("KPIS");

            btnMenuSalir = new Button() { Text = "7. 🚪 Salir", Size = new Size(180, altoBtn), Location = new Point(1455, 6), BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnMenuSalir.Click += BtnMenuSalir_Click;

            pnlNavegacion.Controls.Add(btnMenuUsuarios);
            pnlNavegacion.Controls.Add(btnMenuParametros);
            pnlNavegacion.Controls.Add(btnMenuPruebas);
            pnlNavegacion.Controls.Add(btnMenuReiniciar);
            pnlNavegacion.Controls.Add(btnMenuReportes);
            pnlNavegacion.Controls.Add(btnMenuKpis);
            pnlNavegacion.Controls.Add(btnMenuSalir);
            this.Controls.Add(pnlNavegacion);

            ConstruirVistaUsuarios();
            ConstruirVistaParametros();
            ConstruirVistaPruebas();
            ConstruirVistaReiniciar();
            ConstruirVistaReportes();
            ConstruirVistaKpis();

            MostrarVista("USUARIOS");
        }

        private void ConfigurarFilaHeadersNumerados(DataGridView dgv)
        {
            dgv.RowHeadersWidth = 40;
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

        private void CargarConfiguracionInicial()
        {
            dtpHoraCorteSwift.Value = DateTime.Today.AddHours(ConfiguracionOperativa.HoraCorteSwift);
            dtpHoraCorteRevision.Value = DateTime.Today.AddHours(ConfiguracionOperativa.HoraCorteRevisionManual);
            cmbTipoHorarioSistema.SelectedIndex = ConfiguracionOperativa.UsarHoraUtc ? 1 : 0;

            DateTime fechaSistema = ConfiguracionOperativa.ObtenerFechaHoraActual();
            dtpFechaInicio.Value = new DateTime(fechaSistema.Year, fechaSistema.Month, 1);
            dtpFechaFin.Value = fechaSistema;

            if (chkUsarCarpetaPruebas != null)
            {
                chkUsarCarpetaPruebas.Checked = ConfiguracionOperativa.ModoPruebasActivo;
            }
        }

        private void MostrarVista(string vista)
        {
            pnlVistaUsuarios.Visible = (vista == "USUARIOS");
            pnlVistaParametros.Visible = (vista == "PARAMETROS");
            pnlVistaPruebas.Visible = (vista == "PRUEBAS");
            pnlVistaReiniciar.Visible = (vista == "REINICIAR");
            pnlVistaReportes.Visible = (vista == "REPORTES");
            pnlVistaKpis.Visible = (vista == "KPIS");

            if (vista == "USUARIOS") CargarUsuariosEnGrilla();
            if (vista == "REPORTES" || vista == "KPIS") EjecutarConsultaYActualizacion();
        }

        private void BtnMenuPruebas_Click(object? sender, EventArgs e)
        {
            FormPruebasSistema frmPruebas = new FormPruebasSistema();
            frmPruebas.ShowDialog();
            EjecutarConsultaYActualizacion();
        }

        private void ConstruirVistaUsuarios()
        {
            pnlVistaUsuarios = new Panel() { Size = new Size(1650, 640), Location = new Point(12, 142) };

            GroupBox gpxNuevoUsuario = new GroupBox()
            {
                Text = "Creación y Alta de Usuarios del Sistema",
                Location = new Point(5, 5),
                Size = new Size(480, 625),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            int yPos = 25;
            gpxNuevoUsuario.Controls.Add(new Label() { Text = "Nombre de Usuario (Username):", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            txtNuevoUsername = new TextBox() { Location = new Point(20, yPos + 18), Size = new Size(430, 23), Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxNuevoUsuario.Controls.Add(txtNuevoUsername);

            yPos += 45;
            gpxNuevoUsuario.Controls.Add(new Label() { Text = "Nombre Completo del Usuario:", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            txtNuevoNombreCompleto = new TextBox() { Location = new Point(20, yPos + 18), Size = new Size(430, 23), Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxNuevoUsuario.Controls.Add(txtNuevoNombreCompleto);

            yPos += 45;
            gpxNuevoUsuario.Controls.Add(new Label() { Text = "Compañía / Empresa (Titular Cta):", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            txtNuevoEmpresaTitular = new TextBox() { Location = new Point(20, yPos + 18), Size = new Size(430, 23), Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxNuevoUsuario.Controls.Add(txtNuevoEmpresaTitular);

            yPos += 45;
            gpxNuevoUsuario.Controls.Add(new Label() { Text = "Correo Electrónico (Email):", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            txtNuevoEmail = new TextBox() { Location = new Point(20, yPos + 18), Size = new Size(430, 23), Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            gpxNuevoUsuario.Controls.Add(txtNuevoEmail);

            yPos += 45;
            gpxNuevoUsuario.Controls.Add(new Label() { Text = "Rol Asignado:", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            cmbNuevoRol = new ComboBox() { Location = new Point(20, yPos + 18), Size = new Size(430, 23), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f, FontStyle.Regular) };
            cmbNuevoRol.Items.AddRange(new object[] { "Cliente (Client)", "Ejecutivo (Executive)", "Gerente (Manager)" });
            cmbNuevoRol.SelectedIndex = 0;
            cmbNuevoRol.SelectedIndexChanged += (s, e) => { chkEsClienteCorporativo.Visible = (cmbNuevoRol.SelectedIndex == 0); };
            gpxNuevoUsuario.Controls.Add(cmbNuevoRol);

            yPos += 42;
            chkEsClienteCorporativo = new CheckBox() { Text = "Es Cliente Corporativo (Prioridad Alta)", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.DarkBlue, Visible = true };
            gpxNuevoUsuario.Controls.Add(chkEsClienteCorporativo);

            yPos += 25;
            gpxNuevoUsuario.Controls.Add(new Label() { Text = "Contraseña Temporal Inicial:", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) });
            txtNuevaPasswordInicial = new TextBox() { Location = new Point(20, yPos + 18), Size = new Size(430, 23), Font = new Font("Segoe UI", 9f, FontStyle.Regular), UseSystemPasswordChar = true };
            gpxNuevoUsuario.Controls.Add(txtNuevaPasswordInicial);

            yPos += 45;
            Panel pnlInfoPolitica = new Panel()
            {
                Location = new Point(20, yPos),
                Size = new Size(430, 80),
                BackColor = Color.FromArgb(235, 243, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblPoliticaTexto = new Label()
            {
                Text = "🔒 Requisitos de la Contraseña Inicial:\n" +
                       "• Mínimo 8 caracteres de longitud.\n" +
                       "• Incluir mayúsculas (A-Z) y minúsculas (a-z).\n" +
                       "• Incluir al menos un número (0-9) y un carácter especial (@$!%*?&#).",
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 60, 90),
                Location = new Point(6, 6),
                AutoSize = true
            };
            pnlInfoPolitica.Controls.Add(lblPoliticaTexto);
            gpxNuevoUsuario.Controls.Add(pnlInfoPolitica);

            yPos += 88;
            btnCrearUsuario = new Button()
            {
                Text = "➕ Registrar Usuario (Primer Inicio Forzado)",
                Location = new Point(20, yPos),
                Size = new Size(430, 42),
                BackColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnCrearUsuario.Click += BtnCrearUsuario_Click;
            gpxNuevoUsuario.Controls.Add(btnCrearUsuario);

            pnlVistaUsuarios.Controls.Add(gpxNuevoUsuario);

            GroupBox gpxTablaUsuarios = new GroupBox()
            {
                Text = "Usuarios Registrados y Control de Estado de Acceso",
                Location = new Point(495, 5),
                Size = new Size(1150, 625),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            dgvListaUsuarios = new DataGridView()
            {
                Location = new Point(15, 30),
                Size = new Size(1120, 520),
                AllowUserToAddRows = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            ConfigurarFilaHeadersNumerados(dgvListaUsuarios);
            gpxTablaUsuarios.Controls.Add(dgvListaUsuarios);

            btnToggleEstadoUsuario = new Button()
            {
                Text = "🔄 Cambiar Estado (ACTIVO / NO ACTIVO)",
                Location = new Point(15, 560),
                Size = new Size(270, 45),
                BackColor = Color.Khaki,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnToggleEstadoUsuario.Click += BtnToggleEstadoUsuario_Click;
            gpxTablaUsuarios.Controls.Add(btnToggleEstadoUsuario);

            btnToggleCorporativoUsuario = new Button()
            {
                Text = "🏢 Cambiar Tipo Cliente (CORPORATIVO / REGULAR)",
                Location = new Point(295, 560),
                Size = new Size(330, 45),
                BackColor = Color.LightSkyBlue,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnToggleCorporativoUsuario.Click += BtnToggleCorporativoUsuario_Click;
            gpxTablaUsuarios.Controls.Add(btnToggleCorporativoUsuario);

            btnEliminarUsuario = new Button()
            {
                Text = "❌ Eliminar Seleccionado(s)",
                Location = new Point(635, 560),
                Size = new Size(220, 45),
                BackColor = Color.LightCoral,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnEliminarUsuario.Click += BtnEliminarUsuario_Click;
            gpxTablaUsuarios.Controls.Add(btnEliminarUsuario);

            pnlVistaUsuarios.Controls.Add(gpxTablaUsuarios);
            this.Controls.Add(pnlVistaUsuarios);
        }

        private void BtnCrearUsuario_Click(object? sender, EventArgs e)
        {
            string username = txtNuevoUsername.Text.Trim();
            string nombreCompleto = txtNuevoNombreCompleto.Text.Trim();
            string empresa = txtNuevoEmpresaTitular.Text.Trim();
            string email = txtNuevoEmail.Text.Trim();
            string passInicial = txtNuevaPasswordInicial.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(passInicial) || string.IsNullOrEmpty(empresa))
            {
                MessageBox.Show("Debe ingresar Username, Empresa/Compañía y Contraseña Inicial obligatoriamente.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                email = $"{username}@banco.com";
            }

            UserRole rolSeleccionado = UserRole.Client;
            if (cmbNuevoRol.SelectedIndex == 1) rolSeleccionado = UserRole.Executive;
            else if (cmbNuevoRol.SelectedIndex == 2) rolSeleccionado = UserRole.Manager;

            try
            {
                bool creado = AuthManager.RegisterUser(
                    username: username,
                    password: passInicial,
                    fullName: string.IsNullOrEmpty(nombreCompleto) ? username : nombreCompleto,
                    role: rolSeleccionado,
                    email: email,
                    companyName: empresa,
                    esCorporativo: chkEsClienteCorporativo.Checked,
                    mustChangePassword: true
                );

                if (creado)
                {
                    string tipoCliente = chkEsClienteCorporativo.Checked ? "Cliente Corporativo (Prioridad Alta)" : "Cliente Regular";
                    MessageBox.Show($"Usuario [{username}] de la empresa [{empresa}] registrado exitosamente como [{tipoCliente}].\n\nAl ingresar por primera vez, se le exigirá actualizar su perfil y contraseña.", "Usuario Registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    txtNuevoUsername.Clear();
                    txtNuevoNombreCompleto.Clear();
                    txtNuevoEmpresaTitular.Clear();
                    txtNuevoEmail.Clear();
                    txtNuevaPasswordInicial.Clear();
                    cmbNuevoRol.SelectedIndex = 0;
                    chkEsClienteCorporativo.Checked = false;

                    CargarUsuariosEnGrilla();
                }
                else
                {
                    MessageBox.Show($"No se pudo crear el usuario. Verifique que el nombre [{username}] no exista o que la contraseña cumpla con las políticas de seguridad.", "Error de Registro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear el usuario: {ex.Message}", "Error de Persistencia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarUsuariosEnGrilla()
        {
            try
            {
                var usuarios = AuthManager.ObtenerTodosLosUsuarios();
                dgvListaUsuarios.Columns.Clear();

                var colCheck = new DataGridViewCheckBoxColumn()
                {
                    Name = "Seleccionar",
                    HeaderText = "Seleccionar",
                    Width = 80,
                    ReadOnly = false
                };
                dgvListaUsuarios.Columns.Add(colCheck);

                dgvListaUsuarios.Columns.Add("ID", "ID");
                dgvListaUsuarios.Columns.Add("Username", "Username");
                dgvListaUsuarios.Columns.Add("NombreTitular", "Nombre Titular");
                dgvListaUsuarios.Columns.Add("Empresa", "Compañía / Empresa");
                dgvListaUsuarios.Columns.Add("RolAsignado", "Rol Asignado");
                dgvListaUsuarios.Columns.Add("Email", "Email");
                dgvListaUsuarios.Columns.Add("Corporativo", "Cliente Corporativo");
                dgvListaUsuarios.Columns.Add("EstadoAcceso", "Estado Acceso");
                dgvListaUsuarios.Columns.Add("EstadoCredenciales", "Estado Credenciales");

                for (int i = 1; i < dgvListaUsuarios.Columns.Count; i++)
                {
                    dgvListaUsuarios.Columns[i].ReadOnly = true;
                }

                foreach (var u in usuarios)
                {
                    string estadoAcceso = u.IsActive ? "ACTIVO" : "NO ACTIVO";
                    string estadoCreds = u.MustChangePassword ? "PENDIENTE CAMBIO" : "OK";
                    string empresa = u.SourceHolderName ?? string.Empty;

                    string esCorpStr = "N/A";
                    if (u is UsuarioCliente cli)
                    {
                        esCorpStr = cli.EsCorporativo ? "SÍ (Prioridad 6/8)" : "NO (Prioridad 2/4)";
                    }

                    int index = dgvListaUsuarios.Rows.Add(false, u.UserId, u.Username, u.FullName, empresa, u.Role.ToString(), u.Email, esCorpStr, estadoAcceso, estadoCreds);

                    if (!u.IsActive)
                    {
                        dgvListaUsuarios.Rows[index].DefaultCellStyle.BackColor = Color.MistyRose;
                    }
                    else if (u is UsuarioCliente c && c.EsCorporativo)
                    {
                        dgvListaUsuarios.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(230, 245, 255);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar usuarios: {ex.Message}");
            }
        }

        private void BtnToggleEstadoUsuario_Click(object? sender, EventArgs e)
        {
            var usuarios = AuthManager.ObtenerTodosLosUsuarios();
            int procesados = 0;

            foreach (DataGridViewRow row in dgvListaUsuarios.Rows)
            {
                bool seleccionado = Convert.ToBoolean(row.Cells["Seleccionar"].Value);
                if (seleccionado)
                {
                    string userId = row.Cells["ID"].Value?.ToString() ?? "";
                    var user = usuarios.FirstOrDefault(u => u.UserId == userId);
                    if (user != null)
                    {
                        user.IsActive = !user.IsActive;
                        procesados++;
                    }
                }
            }

            if (procesados > 0)
            {
                UserFileAdapter.GuardarTodos(usuarios);
                MessageBox.Show($"Se ha actualizado el estado de {procesados} usuario(s) exitosamente.", "Estado Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarUsuariosEnGrilla();
            }
            else
            {
                MessageBox.Show("Marque la casilla (checkbox) del usuario que desea cambiar de estado.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnToggleCorporativoUsuario_Click(object? sender, EventArgs e)
        {
            var usuarios = AuthManager.ObtenerTodosLosUsuarios();
            int procesados = 0;

            foreach (DataGridViewRow row in dgvListaUsuarios.Rows)
            {
                bool seleccionado = Convert.ToBoolean(row.Cells["Seleccionar"].Value);
                if (seleccionado)
                {
                    string userId = row.Cells["ID"].Value?.ToString() ?? "";
                    var user = usuarios.FirstOrDefault(u => u.UserId == userId);
                    if (user is UsuarioCliente cliente)
                    {
                        cliente.EsCorporativo = !cliente.EsCorporativo;
                        procesados++;
                    }
                }
            }

            if (procesados > 0)
            {
                UserFileAdapter.GuardarTodos(usuarios);
                MessageBox.Show($"Se ha actualizado la condición corporativa de {procesados} cliente(s) exitosamente.\n\nLos cambios quedaron persistidos en 'usuarios.csv'.", "Tipo de Cliente Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarUsuariosEnGrilla();
            }
            else
            {
                MessageBox.Show("Marque la casilla (checkbox) de un usuario de tipo 'Client' para alternar su condición entre Corporativo y Regular.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnEliminarUsuario_Click(object? sender, EventArgs e)
        {
            var idsAEliminar = new List<string>();

            foreach (DataGridViewRow row in dgvListaUsuarios.Rows)
            {
                bool seleccionado = Convert.ToBoolean(row.Cells["Seleccionar"].Value);
                if (seleccionado)
                {
                    string userId = row.Cells["ID"].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(userId))
                    {
                        idsAEliminar.Add(userId);
                    }
                }
            }

            if (idsAEliminar.Count == 0)
            {
                MessageBox.Show("Marque la casilla (checkbox) del usuario que desea eliminar.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"¿Está seguro de eliminar permanentemente a los {idsAEliminar.Count} usuario(s) seleccionados?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (string id in idsAEliminar)
                {
                    AuthManager.EliminarUsuario(id);
                }

                MessageBox.Show("Usuario(s) eliminado(s) con éxito.", "Eliminación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarUsuariosEnGrilla();
            }
        }

        private void ConstruirVistaParametros()
        {
            pnlVistaParametros = new Panel() { Size = new Size(1650, 640), Location = new Point(12, 142) };
            GroupBox gpxParametros = new GroupBox() { Text = "Parámetros Operativos del Sistema", Location = new Point(470, 80), Size = new Size(710, 420), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            int yPos = 50;
            gpxParametros.Controls.Add(new Label() { Text = "Horario Límite Emisión SWIFT (Corte):", Location = new Point(50, yPos), AutoSize = true });
            dtpHoraCorteSwift = new DateTimePicker() { Location = new Point(400, yPos - 3), Size = new Size(250, 25), Format = DateTimePickerFormat.Time, ShowUpDown = true };
            gpxParametros.Controls.Add(dtpHoraCorteSwift);

            yPos += 70;
            gpxParametros.Controls.Add(new Label() { Text = "Horario Límite Revisión Manual:", Location = new Point(50, yPos), AutoSize = true });
            dtpHoraCorteRevision = new DateTimePicker() { Location = new Point(400, yPos - 3), Size = new Size(250, 25), Format = DateTimePickerFormat.Time, ShowUpDown = true };
            gpxParametros.Controls.Add(dtpHoraCorteRevision);

            yPos += 70;
            gpxParametros.Controls.Add(new Label() { Text = "Modo Horario del Sistema:", Location = new Point(50, yPos), AutoSize = true });
            cmbTipoHorarioSistema = new ComboBox() { Location = new Point(400, yPos - 3), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipoHorarioSistema.Items.AddRange(new object[] { "Hora Local (Sistema)", "Hora Universal (UTC)" });
            gpxParametros.Controls.Add(cmbTipoHorarioSistema);

            yPos += 90;
            btnGuardarParametros = new Button() { Text = "⚙️ Guardar Parámetros", Location = new Point(50, yPos), Size = new Size(600, 55), BackColor = Color.LightGreen, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnGuardarParametros.Click += BtnGuardarParametros_Click;
            gpxParametros.Controls.Add(btnGuardarParametros);

            pnlVistaParametros.Controls.Add(gpxParametros);
            this.Controls.Add(pnlVistaParametros);
        }

        private void BtnGuardarParametros_Click(object? sender, EventArgs e)
        {
            ConfiguracionOperativa.HoraCorteSwift = dtpHoraCorteSwift.Value.Hour;
            ConfiguracionOperativa.HoraCorteRevisionManual = dtpHoraCorteRevision.Value.Hour;
            ConfiguracionOperativa.UsarHoraUtc = (cmbTipoHorarioSistema.SelectedIndex == 1);

            MessageBox.Show("Parámetros operativos actualizados con éxito.", "Configuración Guardada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ConstruirVistaPruebas()
        {
            pnlVistaPruebas = new Panel() { Size = new Size(1650, 640), Location = new Point(12, 142) };
            GroupBox gpxPruebas = new GroupBox() { Text = "Módulo de Pruebas del Sistema", Location = new Point(325, 100), Size = new Size(1000, 400), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            Label lblInfo = new Label()
            {
                Text = "🧪 Presione el botón '3. Pruebas Sistema' en la barra superior para abrir la consola de simulación y pruebas de rendimiento.",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            gpxPruebas.Controls.Add(lblInfo);

            pnlVistaPruebas.Controls.Add(gpxPruebas);
            this.Controls.Add(pnlVistaPruebas);
        }

        private void ConstruirVistaReiniciar()
        {
            pnlVistaReiniciar = new Panel() { Size = new Size(1650, 640), Location = new Point(12, 142) };
            GroupBox gpxReiniciar = new GroupBox() { Text = "Mantenimiento y Mapeo del Sistema", Location = new Point(325, 100), Size = new Size(1000, 400), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            Label lblInfo = new Label()
            {
                Text = "🔄 Módulo de Mantenimiento y Reinicio de Datos Activo.\n\nPuede gestionar los parámetros y purgar logs desde este apartado.",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            gpxReiniciar.Controls.Add(lblInfo);

            pnlVistaReiniciar.Controls.Add(gpxReiniciar);
            this.Controls.Add(pnlVistaReiniciar);
        }

        private void ConstruirVistaReportes()
        {
            pnlVistaReportes = new Panel() { Size = new Size(1650, 640), Location = new Point(12, 142) };
            GroupBox gpxFiltros = new GroupBox() { Text = "Auditoría Global y Selección de Fuente de Datos", Location = new Point(5, 5), Size = new Size(1640, 625), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

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

            xPos += 265;
            gpxFiltros.Controls.Add(new Label() { Text = "Fecha Inicio:", Location = new Point(xPos, 30), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });
            dtpFechaInicio = new DateTimePicker() { Location = new Point(xPos, 55), Size = new Size(150, 25), Format = DateTimePickerFormat.Short };
            gpxFiltros.Controls.Add(dtpFechaInicio);

            xPos += 165;
            gpxFiltros.Controls.Add(new Label() { Text = "Fecha Fin:", Location = new Point(xPos, 30), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });
            dtpFechaFin = new DateTimePicker() { Location = new Point(xPos, 55), Size = new Size(150, 25), Format = DateTimePickerFormat.Short, Enabled = true };
            gpxFiltros.Controls.Add(dtpFechaFin);

            xPos += 165;
            chkUsarCarpetaPruebas = new CheckBox() { Text = "Usar Carpeta Pruebas", Location = new Point(xPos, 57), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.DarkRed };
            gpxFiltros.Controls.Add(chkUsarCarpetaPruebas);

            xPos += 200;
            btnGenerarReporte = new Button() { Text = "🔍 Consultar", Location = new Point(xPos, 53), Size = new Size(180, 30), BackColor = Color.LightSkyBlue, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnGenerarReporte.Click += (s, e) => EjecutarConsultaYActualizacion();
            gpxFiltros.Controls.Add(btnGenerarReporte);

            dgvDetalleReporte = new DataGridView() { Location = new Point(20, 95), Size = new Size(1600, 465), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgvDetalleReporte);
            gpxFiltros.Controls.Add(dgvDetalleReporte);

            btnExportarReporte = new Button() { Text = "📊 Exportar Reporte Global de Auditoría (CSV / Excel)", Location = new Point(20, 570), Size = new Size(1600, 40), BackColor = Color.Gainsboro, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnExportarReporte.Click += BtnExportarReporte_Click;
            gpxFiltros.Controls.Add(btnExportarReporte);

            pnlVistaReportes.Controls.Add(gpxFiltros);
            this.Controls.Add(pnlVistaReportes);
        }

        private void EjecutarConsultaYActualizacion()
        {
            try
            {
                OpcionReporteOperacionActual opcion = (OpcionReporteOperacionActual)(cmbOpcionReporte.SelectedIndex + 1);
                bool esPrueba = chkUsarCarpetaPruebas.Checked;

                if (cmbTipoReporte.SelectedIndex == 0)
                {
                    ultimoReporteGenerado = ReporteManager.ObtenerReporteOperacionActual(dtpFechaInicio.Value, opcion, esPrueba);
                }
                else
                {
                    ultimoReporteGenerado = ReporteManagerAcumulado.ObtenerReporteRangoFechas(dtpFechaInicio.Value, dtpFechaFin.Value, opcion, esPrueba);
                }

                if (ultimoReporteGenerado.DetalleTransacciones != null)
                {
                    dgvDetalleReporte.DataSource = ultimoReporteGenerado.DetalleTransacciones.Select(t => new
                    {
                        ID = t.TransactionId,
                        FechaEmisionSolicitud = t.HoraIngreso.ToString("yyyy-MM-dd HH:mm:ss"),
                        SwiftEmisionInterna = t.SwiftEmisionInternaTimestamp.HasValue ? t.SwiftEmisionInternaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Pendiente",
                        SwiftTransmisionEfectiva = t.SwiftTransmisionEfectivaTimestamp.HasValue ? t.SwiftTransmisionEfectivaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Programada",
                        Origen = t.SourceAccountNumber,
                        Titular = t.SourceHolderName,
                        MontoOriginal = $"{t.Amount:N2} {t.Currency}",
                        MontoUSD = $"$ {t.AmountInUSD:N2}",
                        Destino = t.DestinationAccountNumber,
                        Prioridad = t.PrioridadAsignada,
                        EstadoProceso = t.StatusActual,
                        EstadoTransmisionSWIFT = t.EstadoTransmision,
                        Excepcion = t.HasExceptionFlag ? "SÍ" : "NO",
                        Confirmado = t.Acknowledged ? "SÍ" : "NO"
                    }).ToList();
                }
                else
                {
                    dgvDetalleReporte.DataSource = null;
                }

                var listaGeneral = ultimoReporteGenerado.DetalleTransacciones ?? new List<TransaccionOperacionActualDto>();
                int total = listaGeneral.Count;
                decimal montoTotalUSD = ultimoReporteGenerado.IndicadoresGerenciales?.MontoTotalProcesadoUSD ?? 0m;
                decimal tasaEfectividad = ultimoReporteGenerado.IndicadoresGerenciales?.TasaEfectividadPorcentaje ?? 0m;

                int rechazadas = ultimoReporteGenerado.IndicadoresGerenciales?.TotalRechazadas ?? 0;
                int excepciones = ultimoReporteGenerado.IndicadoresGerenciales?.TotalExcepcionesSistema ?? 0;
                double pctExcepcionesRechazos = total > 0 ? (double)(rechazadas + excepciones) / total * 100.0 : 0.0;

                lblKpiTotalSolicitudes.Text = total.ToString();
                lblKpiMontoProcesado.Text = $"$ {montoTotalUSD:N2}";
                lblKpiTasaEfectividad.Text = $"{tasaEfectividad:F1}%";
                lblKpiPorcentajeExcepciones.Text = $"{pctExcepcionesRechazos:F1}%";

                var resumenGrilla = listaGeneral.GroupBy(t => t.StatusActual)
                    .Select(g => new { Estado = g.Key, Cantidad = g.Count(), Porcentaje = $"{(double)g.Count() / (total > 0 ? total : 1) * 100:F1}%", MontoUSD = g.Sum(x => x.AmountInUSD) }).ToList();

                dgvResumenKpisAdmin.DataSource = resumenGrilla;

                if (panelGraficoEstado != null)
                {
                    panelGraficoEstado.CargarDatosDoughnut(resumenGrilla.Select(x => (x.Estado, x.Cantidad)).ToList());
                }

                if (panelGraficoMonedas != null)
                {
                    var dictMonedas = ultimoReporteGenerado.IndicadoresGerenciales?.DistribucionMontosPorMoneda ?? new Dictionary<string, decimal>();
                    panelGraficoMonedas.CargarDatosPieMonedas(dictMonedas.Select(k => (k.Key, k.Value)).ToList());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el reporte: {ex.Message}", "Error de Consulta", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportarReporte_Click(object? sender, EventArgs e)
        {
            if (ultimoReporteGenerado?.DetalleTransacciones == null)
            {
                MessageBox.Show("Primero debe realizar una consulta válida.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using SaveFileDialog sfd = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"ReporteAdmin_{ConfiguracionOperativa.ObtenerFechaHoraActual():yyyy-MM-dd_HHmm}.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                    sw.WriteLine($"Reporte: {ultimoReporteGenerado.TipoReporte}");
                    sw.WriteLine($"Fecha Consulta: {ultimoReporteGenerado.FechaConsultaUtc:yyyy-MM-dd HH:mm:ss}\n");
                    sw.WriteLine("TransactionId;FechaEmisionSolicitud;SwiftEmisionInterna;SwiftTransmisionEfectiva;SourceHolder;SourceAccount;Amount;Currency;AmountInUSD;DestinationBank;DestinationAccount;Prioridad;StatusProceso;EstadoTransmisionSWIFT;HasException;Acknowledged");
                    foreach (var t in ultimoReporteGenerado.DetalleTransacciones)
                    {
                        string emisionStr = t.SwiftEmisionInternaTimestamp.HasValue ? t.SwiftEmisionInternaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                        string transmisionStr = t.SwiftTransmisionEfectivaTimestamp.HasValue ? t.SwiftTransmisionEfectivaTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";

                        sw.WriteLine($"{t.TransactionId};{t.HoraIngreso:yyyy-MM-dd HH:mm:ss};{emisionStr};{transmisionStr};\"{t.SourceHolderName}\";{t.SourceAccountNumber};{t.Amount};{t.Currency};{t.AmountInUSD};{t.DestinationBankName};{t.DestinationAccountNumber};{t.PrioridadAsignada};{t.StatusActual};{t.EstadoTransmision};{t.HasExceptionFlag};{t.Acknowledged}");
                    }
                    MessageBox.Show("Reporte exportado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ConstruirVistaKpis()
        {
            pnlVistaKpis = new Panel() { Size = new Size(1650, 640), Location = new Point(12, 142) };

            pnlVistaKpis.Controls.Add(CrearTarjetaKpi("Total Solicitudes", out lblKpiTotalSolicitudes, Color.SteelBlue, 10, 10, 390));
            pnlVistaKpis.Controls.Add(CrearTarjetaKpi("Monto Procesado (USD)", out lblKpiMontoProcesado, Color.SeaGreen, 420, 10, 390));
            pnlVistaKpis.Controls.Add(CrearTarjetaKpi("Tasa Efectividad Global", out lblKpiTasaEfectividad, Color.DarkGoldenrod, 830, 10, 390));
            pnlVistaKpis.Controls.Add(CrearTarjetaKpi("% Excepciones / Rechazos", out lblKpiPorcentajeExcepciones, Color.Coral, 1240, 10, 390));

            GroupBox gpxKpiTabla = new GroupBox() { Text = "Desglose por Estado Operativo", Location = new Point(10, 150), Size = new Size(530, 480), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            dgvResumenKpisAdmin = new DataGridView() { Location = new Point(15, 30), Size = new Size(500, 435), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            ConfigurarFilaHeadersNumerados(dgvResumenKpisAdmin);
            gpxKpiTabla.Controls.Add(dgvResumenKpisAdmin);
            pnlVistaKpis.Controls.Add(gpxKpiTabla);

            GroupBox gpxGraficoEstado = new GroupBox() { Text = "Gráfico 1: Transacciones por Estado (Doughnut)", Location = new Point(550, 150), Size = new Size(530, 480), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            panelGraficoEstado = new PanelGraficoKpi() { Location = new Point(15, 30), Size = new Size(500, 435), BackColor = Color.White };
            gpxGraficoEstado.Controls.Add(panelGraficoEstado);
            pnlVistaKpis.Controls.Add(gpxGraficoEstado);

            GroupBox gpxGraficoMonedas = new GroupBox() { Text = "Gráfico 2: Montos Totales en USD por Moneda (Pie)", Location = new Point(1090, 150), Size = new Size(540, 480), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            panelGraficoMonedas = new PanelGraficoPieMonedas() { Location = new Point(15, 30), Size = new Size(510, 435), BackColor = Color.White };
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

        private void BtnMenuSalir_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("¿Desea salir del Panel Administrador?", "Salir", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                AuthManager.UsuarioActual = null;
                this.Close();
            }
        }
    }
}