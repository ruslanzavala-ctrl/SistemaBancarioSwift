namespace SistemaBancario.Models
{
    public class UsuarioEjecutivo : User
    {
        public required string Nombres { get; set; }
        public required string Apellidos { get; set; }
        public required string CodigoEmpleado { get; set; }
        public string Departamento { get; set; } = "Operaciones y Cumplimiento";
    }
}