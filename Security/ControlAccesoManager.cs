namespace SistemaBancario.Security
{
    public enum RolUsuario
    {
        Cliente,
        Ejecutivo,
        Gerente,
        IngenieroSoftware
    }

    public static class ControlAccesoManager
    {
        /// <summary>
        /// Valida si un rol tiene permiso para consultar un rango de fechas histórico.
        /// - Cliente: No usa reportes globales.
        /// - Ejecutivo: Solo día actual y día anterior.
        /// - Gerente / Ingeniero: Acceso ilimitado a cualquier rango.
        /// </summary>
        public static bool ValidarRangoFechas(RolUsuario rol, DateTime fechaInicio, DateTime fechaFin)
        {
            DateTime hoy = DateTime.Today;

            switch (rol)
            {
                case RolUsuario.Cliente:
                    return false; // El cliente solo ve su propia cuenta mediante consultas filtradas por su ID

                case RolUsuario.Ejecutivo:
                    DateTime limitePermitido = hoy.AddDays(-1);
                    return fechaInicio.Date >= limitePermitido && fechaFin.Date <= hoy;

                case RolUsuario.Gerente:
                case RolUsuario.IngenieroSoftware:
                    return true; // Acceso ilimitado

                default:
                    return false;
            }
        }

        /// <summary>
        /// Valida si el rol tiene privilegios para aprobar transacciones observadas.
        /// Solo el Gerente y el Ingeniero de Software tienen esta autorización.
        /// </summary>
        public static bool PuedeAprobarTransacciones(RolUsuario rol)
        {
            return rol == RolUsuario.Gerente || rol == RolUsuario.IngenieroSoftware;
        }

        /// <summary>
        /// Valida si el rol puede ver la consulta en Tiempo Real (Foto del Proceso).
        /// </summary>
        public static bool PuedeVerTiempoReal(RolUsuario rol)
        {
            return rol == RolUsuario.Ejecutivo || rol == RolUsuario.Gerente || rol == RolUsuario.IngenieroSoftware;
        }
    }
}