namespace SistemaBancario.Models
{
    public class UsuarioManager : User
    {
        // Datos de Identificación y Acceso
        public required string Nombres { get; set; }
        public required string Apellidos { get; set; }
        public required string CodigoEmpleado { get; set; }
        public string Departamento { get; set; } = "Gerencia de Operaciones";
    }
}