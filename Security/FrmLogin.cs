using SistemaBancario.Models;

namespace SistemaBancario.Security
{
    public partial class FrmLogin : Form
    {
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private Label lblUser = null!;
        private Label lblPass = null!;

        public User? AuthenticatedUser { get; private set; }

        public FrmLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Sistema Bancario - Inicio de Sesión";
            this.Size = new System.Drawing.Size(350, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            lblUser = new Label { Text = "Usuario:", Location = new System.Drawing.Point(30, 30), AutoSize = true };
            txtUsername = new TextBox { Location = new System.Drawing.Point(30, 50), Width = 260 };

            lblPass = new Label { Text = "Contraseña:", Location = new System.Drawing.Point(30, 90), AutoSize = true };
            txtPassword = new TextBox { Location = new System.Drawing.Point(30, 110), Width = 260, PasswordChar = '*' };

            btnLogin = new Button { Text = "Ingresar", Location = new System.Drawing.Point(130, 160), Size = new System.Drawing.Size(100, 30) };
            btnLogin.Click += BtnLogin_Click;

            this.Controls.Add(lblUser);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (AuthManager.Authenticate(username, password))
            {
                var usuario = AuthManager.UsuarioActual;

                // Si el usuario requiere cambiar su contraseña en su primer inicio
                if (usuario != null && usuario.MustChangePassword)
                {
                    MessageBox.Show("Es su primer inicio de sesión o se ha solicitado un cambio de clave obligatorio. Por favor, actualice su contraseña.",
                                    "Cambio de Contraseña Requerido", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    using var frmCambio = new FrmCambiarPassword(usuario.Username);
                    if (frmCambio.ShowDialog() == DialogResult.OK)
                    {
                        usuario.MustChangePassword = false;
                        UserFileAdapter.GuardarTodos(AuthManager.ObtenerTodosLosUsuarios());
                    }
                    else
                    {
                        MessageBox.Show("No se puede iniciar sesión sin actualizar la contraseña.", "Acceso Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        AuthManager.UsuarioActual = null;
                        return;
                    }
                }

                var mfaForm = new FrmMfaVerify(username);
                if (mfaForm.ShowDialog() == DialogResult.OK)
                {
                    AuthenticatedUser = AuthManager.UsuarioActual;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Verificación MFA cancelada o incorrecta.", "Seguridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Autenticación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}