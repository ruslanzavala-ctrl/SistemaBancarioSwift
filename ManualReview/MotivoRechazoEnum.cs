namespace SistemaBancario.Models
{
    public enum MotivoRechazoBancario
    {
        MontoInusualNoFrecuente,
        AlertaRiesgoFraude,
        FaltaSustentoDocumentalFiscal,
        CuentaOrigenDestinoSospechosa,
        IncumplimientoPoliticaCompliance,
        RestriccionOperativaTemporal
    }

    public static class ExtensionMotivosRechazo
    {
        public static string ObtenerDescripcion(this MotivoRechazoBancario motivo)
        {
            return motivo switch
            {
                MotivoRechazoBancario.MontoInusualNoFrecuente => "Monto elevado o no frecuente según el perfil transaccional del cliente.",
                MotivoRechazoBancario.AlertaRiesgoFraude => "Activación de bandera roja por patrón de riesgo operativo o fraude.",
                MotivoRechazoBancario.FaltaSustentoDocumentalFiscal => "Falta de respaldo documental sobre el origen y destino de los fondos.",
                MotivoRechazoBancario.CuentaOrigenDestinoSospechosa => "La cuenta vinculada presenta restricciones de seguridad o reportes previos.",
                MotivoRechazoBancario.IncumplimientoPoliticaCompliance => "Incumplimiento de normativas internas de cumplimiento bancario y antilavado.",
                MotivoRechazoBancario.RestriccionOperativaTemporal => "Restricción temporal aplicada sobre el segmento de cuentas corporativas.",
                _ => "Motivo de seguridad estándar no especificado."
            };
        }
    }
}