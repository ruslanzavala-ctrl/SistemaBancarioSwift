namespace SistemaBancario.Models
{
    public class UsuarioCliente : User
    {
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;

        // Sincronización bi-direccional automática entre CompanyName y SourceHolderName
        public string CompanyName
        {
            get => SourceHolderName;
            set => SourceHolderName = value;
        }

        public string Cargo { get; set; } = "Titular";

        // Flag explícito para asignación de prioridad alta
        public bool EsCorporativo { get; set; } = false;
    }
}