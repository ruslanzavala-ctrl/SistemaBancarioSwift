namespace SistemaBancario.Models
{
    public class User
    {
        public string UserId { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }

        // Datos Personales del Usuario
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Compañía / Empresa Titular de las Cuentas
        public string SourceHolderName { get; set; } = string.Empty;

        // Control de Estado y Acceso
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; } = true;

        public string EstadoAcceso => IsActive ? "ACTIVO" : "NO ACTIVO";
        public string EstadoPassword => MustChangePassword ? "PENDIENTE CAMBIO" : "OK";
    }
}