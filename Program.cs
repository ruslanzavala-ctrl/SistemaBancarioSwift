using SistemaBancario.Models;
using SistemaBancario.Security;
using SistemaBancario.UI;

namespace Sistema_Bancario_de_gestión_de_transacciones_en_C_
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Inicializamos la aplicación y sus configuraciones
            ApplicationConfiguration.Initialize();

            // Instanciamos el formulario de Login
            var loginForm = new FrmLogin();

            // Si el login y el código MFA son correctos (DialogResult.OK)
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                var usuario = loginForm.AuthenticatedUser;

                if (usuario != null)
                {
                    // Asignamos la sesión activa global
                    AuthManager.UsuarioActual = usuario;

                    MessageBox.Show($"¡Bienvenido al sistema, {usuario.Username}!\nRol asignado: {usuario.Role}",
                        "Sesión Iniciada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Redirección dinámica según el rol del usuario
                    switch (usuario.Role)
                    {
                        case UserRole.Client:
                            Application.Run(new FormClientePrincipal());
                            break;

                        case UserRole.Executive:
                            Application.Run(new FormEjecutivoPrincipal());
                            break;

                        case UserRole.Admin:
                            Application.Run(new FormAdminPrincipal());
                            break;

                        case UserRole.Manager:
                            Application.Run(new FormManagerPrincipal());
                            break;

                        default:
                            MessageBox.Show("Rol no reconocido.", "Error de Acceso", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                            // Puedes ejecutar tu prueba models y seguridad temporal inicial 
                            //ModuleTester.RunTests();

                            // Y/O Puedes ejecutar tu prueba original de colas:
                            //TestDataStructures.EjecutarPruebaColas();

                            // Y/O ejecutar la prueba integral de modelos y SWIFT:
                            //TestDataStructuresUI.EjecutarPruebaColasUI();

                            // Y/O ejecutar prueba de modulos de revision de transacciones:
                            //TestModuloRevisionUI.EjecutarPruebaModuloRevisionConCatalogo();

                            //y/o  Ejecutar la prueba E2E completa
                            //SistemaBancario.Tests.PruebaEndToEndSistemaBancario.EjecutarPruebaExhaustiva();

                            //y/o  Ejecutar la prueba E2E completa en tiempo real con generacion de reporte
                            //SistemaBancario.Tests.PruebaEndToEndTiempoReal.EjecutarConFormulario();

                            //Prueba de generación general de transacciones y persistencia masiva directa
                            //SistemaBancario.Tests.PruebaPersistenciaMasivaDirecta.EjecutarPrueba();

                            //Prueba de generación general de transacciones y persistencia masiva directa con generacion de reporte
                            //SistemaBancario.Tests.PruebaReporteManager.EjecutarPrueba();



                            // Nota: Aquí posteriormente abriremos tu ventana principal o el menú de transacciones
                            // Ejemplo: Application.Run(new FrmMain());
                    }
                }
            }
        }
    }
}