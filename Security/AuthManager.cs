using SistemaBancario.Models;

namespace SistemaBancario.Security
{
    public static class AuthManager
    {
        // Almacenamiento respaldado por el adaptador CSV en disco
        private static List<User> _users;

        // Usuario actualmente autenticado en la sesión activa
        public static User? UsuarioActual { get; set; }

        static AuthManager()
        {
            // Cargar usuarios desde el archivo CSV con persistencia automática
            _users = UserFileAdapter.CargarUsuarios();

            // Sesión inicial por defecto para pruebas rápidas
            if (_users.Count > 0)
            {
                UsuarioActual = _users[0];
            }
        }

        /// <summary>
        /// Recarga los usuarios desde el archivo CSV si se realizaron cambios externos.
        /// </summary>
        public static void RecargarUsuarios()
        {
            _users = UserFileAdapter.CargarUsuarios();
        }

        /// <summary>
        /// Autenticar usuario validando credenciales y estableciendo la sesión activa.
        /// </summary>
        public static bool Authenticate(string username, string password)
        {
            var user = _users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return false;

            // Validación de estado activo: Si está inactivo, no se permite el acceso
            if (!user.IsActive) return false;

            bool isValid = PasswordHasher.VerifyPassword(password, user.PasswordHash);
            if (isValid)
            {
                UsuarioActual = user; // Fijamos la sesión activa
            }
            return isValid;
        }

        /// <summary>
        /// Aprovisionamiento de nuevos usuarios por el superusuario (Admin) con persistencia en CSV.
        /// Permite definir si el cliente es de tipo Corporativo para asignación de prioridad alta (Prioridad 5).
        /// </summary>
        public static bool RegisterUser(
            string username,
            string password,
            string fullName,
            UserRole role,
            string email = "",
            string companyName = "",
            bool esCorporativo = false,
            bool mustChangePassword = true)
        {
            if (_users.Exists(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))) return false;

            if (!PasswordValidator.ValidatePolicy(password)) return false;

            string emailFinal = string.IsNullOrEmpty(email) ? $"{username}@ejemplo.com" : email;
            string hash = PasswordHasher.HashPassword(password);

            // Separación de Nombres / Apellidos para los modelos derivados
            string[] partesNombre = fullName.Split(' ');
            string nom = partesNombre.Length > 0 ? partesNombre[0] : fullName;
            string ape = partesNombre.Length > 1 ? string.Join(" ", partesNombre, 1, partesNombre.Length - 1) : "SinApellido";

            User nuevoUsuario;

            switch (role)
            {
                case UserRole.Client:
                    nuevoUsuario = new UsuarioCliente
                    {
                        Username = username,
                        PasswordHash = hash,
                        Role = role,
                        FullName = fullName,
                        Email = emailFinal,
                        Nombres = nom,
                        Apellidos = ape,
                        CompanyName = string.IsNullOrEmpty(companyName) ? "Empresa Cliente SAC" : companyName,
                        Cargo = "Titular",
                        EsCorporativo = esCorporativo
                    };
                    break;

                case UserRole.Manager:
                    nuevoUsuario = new UsuarioManager
                    {
                        Username = username,
                        PasswordHash = hash,
                        Role = role,
                        FullName = fullName,
                        Email = emailFinal,
                        Nombres = nom,
                        Apellidos = ape,
                        CodigoEmpleado = string.IsNullOrEmpty(companyName) ? "MGR-001" : companyName
                    };
                    break;

                case UserRole.Executive:
                    nuevoUsuario = new UsuarioEjecutivo
                    {
                        Username = username,
                        PasswordHash = hash,
                        Role = role,
                        FullName = fullName,
                        Email = emailFinal,
                        Nombres = nom,
                        Apellidos = ape,
                        CodigoEmpleado = string.IsNullOrEmpty(companyName) ? "EJC-001" : companyName
                    };
                    break;

                default:
                    nuevoUsuario = new User
                    {
                        Username = username,
                        PasswordHash = hash,
                        Role = role,
                        FullName = fullName,
                        Email = emailFinal
                    };
                    break;
            }

            // Asignamos el flag de cambio obligatorio de clave
            nuevoUsuario.MustChangePassword = mustChangePassword;

            _users.Add(nuevoUsuario);

            // Guardar cambios automáticamente en el archivo CSV
            UserFileAdapter.GuardarTodos(_users);

            return true;
        }

        /// <summary>
        /// Retorna la lista completa de usuarios cargados en el sistema.
        /// </summary>
        public static List<User> ObtenerTodosLosUsuarios()
        {
            return _users;
        }

        /// <summary>
        /// Actualización segura de credenciales con persistencia en disco.
        /// </summary>
        public static bool UpdatePassword(string username, string currentPassword, string newPassword)
        {
            var user = _users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return false;

            if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash)) return false;
            if (!PasswordValidator.ValidatePolicy(newPassword)) return false;

            user.PasswordHash = PasswordHasher.HashPassword(newPassword);

            // Persistir cambios en disco
            UserFileAdapter.GuardarTodos(_users);
            return true;
        }
        /// <summary>
        /// Permite eliminar un usuario por su ID único y guardar la lista actualizada en el CSV.
        /// </summary>
        public static bool EliminarUsuario(string userId)
        {
            var usuario = _users.Find(u => u.UserId == userId);
            if (usuario != null)
            {
                _users.Remove(usuario);
                UserFileAdapter.GuardarTodos(_users);
                return true;
            }
            return false;
        }

        // =========================================================================
        // VALIDACIONES DE ACCESO Y ROLES PARA EL MÓDULO 7 (REPORTES Y CONSULTAS)
        // =========================================================================

        public static bool PuedeVerTiempoReal()
        {
            if (UsuarioActual == null) return false;
            return UsuarioActual.Role == UserRole.Executive ||
                   UsuarioActual.Role == UserRole.Manager ||
                   UsuarioActual.Role == UserRole.Admin;
        }

        public static bool ValidarPermisoRangoFechas(DateTime fechaInicio, DateTime fechaFin)
        {
            if (UsuarioActual == null) return false;

            if (UsuarioActual.Role == UserRole.Client ||
                UsuarioActual.Role == UserRole.Manager ||
                UsuarioActual.Role == UserRole.Admin)
            {
                return true;
            }

            if (UsuarioActual.Role == UserRole.Executive)
            {
                DateTime hoy = DateTime.Today;
                DateTime limitePermitido = hoy.AddDays(-1);
                return fechaInicio.Date >= limitePermitido && fechaFin.Date <= hoy;
            }

            return false;
        }

        public static bool PuedeAprobarObservadas()
        {
            if (UsuarioActual == null) return false;
            return UsuarioActual.Role == UserRole.Executive ||
                   UsuarioActual.Role == UserRole.Manager;
        }
    }
}