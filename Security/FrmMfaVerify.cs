namespace SistemaBancario.Security
{
    public partial class FrmMfaVerify : Form
    {
        private readonly string _generatedOtp;
        private TextBox txtOtpCode = null!;
        private Button btnVerify = null!;
        private Label lblInstruction = null!;

        public FrmMfaVerify(string username)
        {
            InitializeComponent();

            _generatedOtp = "123456";

            MessageBox.Show($"Se ha enviado un código OTP al dispositivo de: {username}\n(Código de prueba: {_generatedOtp})",
                            "Verificación MFA", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InitializeComponent()
        {
            this.Text = "Autenticación Multifactor (MFA)";
            this.Size = new System.Drawing.Size(320, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            lblInstruction = new Label { Text = "Ingrese el código OTP de 6 dígitos:", Location = new System.Drawing.Point(30, 25), AutoSize = true };
            txtOtpCode = new TextBox { Location = new System.Drawing.Point(30, 55), Width = 240 };

            btnVerify = new Button { Text = "Verificar", Location = new System.Drawing.Point(110, 100), Size = new System.Drawing.Size(90, 30) };
            btnVerify.Click += BtnVerify_Click;

            this.Controls.Add(lblInstruction);
            this.Controls.Add(txtOtpCode);
            this.Controls.Add(btnVerify);
        }

        private void BtnVerify_Click(object? sender, EventArgs e)
        {
            if (txtOtpCode.Text.Trim() == _generatedOtp)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("El código OTP ingresado es incorrecto.", "Error MFA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}