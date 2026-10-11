namespace SistemaBancario.Models
{
    public enum UserRole
    {
        Admin,      // Ingeniero de Software / Administrador general
        Executive,  // Ejecutivo (acceso en tiempo real y días permitidos)
        Client,     // Cliente (restringido a su propia cuenta)
        Manager     // Gerente (nuevo rol con permisos de aprobación y rangos libres)
    }
}