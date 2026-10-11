namespace SistemaBancario.Security
{
    public partial class FrmCambiarPassword : Form
    {
        private readonly string _username;
        private TextBox txtCurrentPassword = null!;
        private TextBox txtNewPassword = null!;
        private TextBox txtConfirmPassword = null!;
        private Button btnChange = null!;

        public FrmCambiarPassword(string username)
        {
            InitializeComponent();
            _username = username;
        }

        private void InitializeComponent()
        {
            this.Text = "Cambio de Contraseña";
            this.Size = new System.Drawing.Size(350, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lbl1 = new Label { Text = "Contraseña actual:", Location = new System.Drawing.Point(30, 20), AutoSize = true };
            txtCurrentPassword = new TextBox { Location = new System.Drawing.Point(30, 40), Width = 260, PasswordChar = '*' };

            var lbl2 = new Label { Text = "Nueva contraseña:", Location = new System.Drawing.Point(30, 80), AutoSize = true };
            txtNewPassword = new TextBox { Location = new System.Drawing.Point(30, 100), Width = 260, PasswordChar = '*' };

            var lbl3 = new Label { Text = "Confirmar nueva contraseña:", Location = new System.Drawing.Point(30, 140), AutoSize = true };
            txtConfirmPassword = new TextBox { Location = new System.Drawing.Point(30, 160), Width = 260, PasswordChar = '*' };

            btnChange = new Button { Text = "Actualizar", Location = new System.Drawing.Point(120, 210), Size = new System.Drawing.Size(100, 30) };
            btnChange.Click += BtnChange_Click;

            this.Controls.Add(lbl1);
            this.Controls.Add(txtCurrentPassword);
            this.Controls.Add(lbl2);
            this.Controls.Add(txtNewPassword);
            this.Controls.Add(lbl3);
            this.Controls.Add(txtConfirmPassword);
            this.Controls.Add(btnChange);
        }

        private void BtnChange_Click(object? sender, EventArgs e)
        {
            string currentPass = txtCurrentPassword.Text;
            string newPass = txtNewPassword.Text;
            string confirmPass = txtConfirmPassword.Text;

            if (newPass != confirmPass)
            {
                MessageBox.Show("La nueva contraseña y la confirmación no coinciden.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = AuthManager.UpdatePassword(_username, currentPass, newPass);

            if (success)
            {
                MessageBox.Show("Contraseña actualizada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("No se pudo actualizar. Verifique su contraseña actual y que la nueva cumpla los requisitos de seguridad.",
                                "Error de Seguridad", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}