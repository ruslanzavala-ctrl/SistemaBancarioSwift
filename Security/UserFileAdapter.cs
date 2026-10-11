using SistemaBancario.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SistemaBancario.Security
{
    public static class UserFileAdapter
    {
        private static readonly string RutaDirectorio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string RutaArchivo = Path.Combine(RutaDirectorio, "usuarios.csv");

        /// <summary>
        /// Carga los usuarios instanciando su tipo concreto, mapeando SourceHolderName y leyendo la bandera EsCorporativo.
        /// </summary>
        public static List<User> CargarUsuarios()
        {
            var listaUsuarios = new List<User>();

            try
            {
                if (!Directory.Exists(RutaDirectorio))
                {
                    Directory.CreateDirectory(RutaDirectorio);
                }

                if (!File.Exists(RutaArchivo))
                {
                    listaUsuarios = ObtenerUsuariosPorDefecto();
                    GuardarTodos(listaUsuarios);
                    return listaUsuarios;
                }

                var lineas = File.ReadAllLines(RutaArchivo, Encoding.UTF8);
                for (int i = 1; i < lineas.Length; i++) // Omitir la cabecera
                {
                    var linea = lineas[i];
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var partes = linea.Split(';');
                    if (partes.Length >= 6)
                    {
                        if (Enum.TryParse<UserRole>(partes[2], out var rol))
                        {
                            string username = partes[0].Trim();
                            string passwordHash = partes[1].Trim();
                            string fullName = partes[3].Trim().Trim('"');
                            string email = partes[4].Trim();
                            string sourceHolderName = partes[5].Trim().Trim('"');

                            bool isActive = partes.Length > 6 ? (bool.TryParse(partes[6].Trim(), out var act) ? act : true) : true;
                            bool mustChangePassword = partes.Length > 7 ? (bool.TryParse(partes[7].Trim(), out var mcp) ? mcp : false) : false;
                            bool esCorporativo = partes.Length > 8 ? (bool.TryParse(partes[8].Trim(), out var corp) ? corp : false) : false;

                            string[] partesNombre = fullName.Split(' ');
                            string nom = partesNombre.Length > 0 ? partesNombre[0] : fullName;
                            string ape = partesNombre.Length > 1 ? string.Join(" ", partesNombre, 1, partesNombre.Length - 1) : "SinApellido";

                            User usuarioCreado;

                            switch (rol)
                            {
                                case UserRole.Client:
                                    usuarioCreado = new UsuarioCliente
                                    {
                                        Username = username,
                                        PasswordHash = passwordHash,
                                        Role = rol,
                                        FullName = fullName,
                                        Email = email,
                                        Nombres = nom,
                                        Apellidos = ape,
                                        SourceHolderName = string.IsNullOrEmpty(sourceHolderName) ? "Empresa Cliente SAC" : sourceHolderName,
                                        Cargo = "Titular",
                                        EsCorporativo = esCorporativo
                                    };
                                    break;

                                case UserRole.Manager:
                                    usuarioCreado = new UsuarioManager
                                    {
                                        Username = username,
                                        PasswordHash = passwordHash,
                                        Role = rol,
                                        FullName = fullName,
                                        Email = email,
                                        Nombres = nom,
                                        Apellidos = ape,
                                        CodigoEmpleado = string.IsNullOrEmpty(sourceHolderName) ? "MGR-001" : sourceHolderName,
                                        SourceHolderName = sourceHolderName
                                    };
                                    break;

                                case UserRole.Executive:
                                    usuarioCreado = new UsuarioEjecutivo
                                    {
                                        Username = username,
                                        PasswordHash = passwordHash,
                                        Role = rol,
                                        FullName = fullName,
                                        Email = email,
                                        Nombres = nom,
                                        Apellidos = ape,
                                        CodigoEmpleado = string.IsNullOrEmpty(sourceHolderName) ? "EJC-001" : sourceHolderName,
                                        SourceHolderName = sourceHolderName
                                    };
                                    break;

                                default:
                                    usuarioCreado = new User
                                    {
                                        Username = username,
                                        PasswordHash = passwordHash,
                                        Role = rol,
                                        FullName = fullName,
                                        Email = email,
                                        SourceHolderName = sourceHolderName
                                    };
                                    break;
                            }

                            usuarioCreado.IsActive = isActive;
                            usuarioCreado.MustChangePassword = mustChangePassword;

                            listaUsuarios.Add(usuarioCreado);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar usuarios desde disco: {ex.Message}");
                listaUsuarios = ObtenerUsuariosPorDefecto();
            }

            return listaUsuarios;
        }

        public static List<User> CargarTodos() => CargarUsuarios();

        /// <summary>
        /// Guarda todos los usuarios persistiendo las columnas SourceHolderName y EsCorporativo.
        /// </summary>
        public static void GuardarTodos(List<User> usuarios)
        {
            try
            {
                if (!Directory.Exists(RutaDirectorio))
                {
                    Directory.CreateDirectory(RutaDirectorio);
                }

                var sb = new StringBuilder();
                sb.AppendLine("Username;PasswordHash;Role;FullName;Email;SourceHolderName;IsActive;MustChangePassword;EsCorporativo");

                foreach (var u in usuarios)
                {
                    string sourceHolderName = u.SourceHolderName ?? string.Empty;
                    bool esCorp = false;

                    if (u is UsuarioCliente cliente)
                    {
                        if (string.IsNullOrEmpty(sourceHolderName)) sourceHolderName = cliente.CompanyName ?? string.Empty;
                        esCorp = cliente.EsCorporativo;
                    }
                    else if (u is UsuarioManager manager)
                    {
                        if (string.IsNullOrEmpty(sourceHolderName)) sourceHolderName = manager.CodigoEmpleado ?? string.Empty;
                    }
                    else if (u is UsuarioEjecutivo ejecutivo)
                    {
                        if (string.IsNullOrEmpty(sourceHolderName)) sourceHolderName = ejecutivo.CodigoEmpleado ?? string.Empty;
                    }

                    sb.AppendLine($"{u.Username};{u.PasswordHash};{u.Role};\"{u.FullName}\";{u.Email};\"{sourceHolderName}\";{u.IsActive};{u.MustChangePassword};{esCorp}");
                }

                File.WriteAllText(RutaArchivo, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar usuarios en disco: {ex.Message}");
            }
        }

        private static List<User> ObtenerUsuariosPorDefecto()
        {
            return new List<User>
            {
                new User
                {
                    Username = "admin_test",
                    PasswordHash = PasswordHasher.HashPassword("admin123456"),
                    Role = UserRole.Admin,
                    FullName = "Ingeniero de Software (Master)",
                    Email = "ingeniero@ejemplo.com",
                    SourceHolderName = "ADMIN-001",
                    IsActive = true,
                    MustChangePassword = false
                },
                new UsuarioManager
                {
                    Username = "gerente_test",
                    PasswordHash = PasswordHasher.HashPassword("gerente123456"),
                    Role = UserRole.Manager,
                    FullName = "Carlos Gerente",
                    Email = "gerente.test@ejemplo.com",
                    Nombres = "Carlos",
                    Apellidos = "Gerente",
                    CodigoEmpleado = "MGR-001",
                    SourceHolderName = "MGR-001",
                    IsActive = true,
                    MustChangePassword = false
                },
                new UsuarioEjecutivo
                {
                    Username = "ejecutivo_test",
                    PasswordHash = PasswordHasher.HashPassword("ejecutivo123456"),
                    Role = UserRole.Executive,
                    FullName = "Ana Ejecutivo",
                    Email = "ejecutivo.test@ejemplo.com",
                    Nombres = "Ana",
                    Apellidos = "Ejecutivo",
                    CodigoEmpleado = "EJC-001",
                    SourceHolderName = "EJC-001",
                    IsActive = true,
                    MustChangePassword = false
                },
                new UsuarioCliente
                {
                    Username = "cliente_test",
                    PasswordHash = PasswordHasher.HashPassword("cliente123456"),
                    Role = UserRole.Client,
                    FullName = "Juan Cliente",
                    Email = "cliente.test@ejemplo.com",
                    Nombres = "Juan",
                    Apellidos = "Cliente",
                    CompanyName = "Empresa Cliente SAC",
                    SourceHolderName = "Empresa Cliente SAC",
                    Cargo = "Titular",
                    EsCorporativo = true, // Cliente por defecto corporativo para pruebas
                    IsActive = true,
                    MustChangePassword = false
                }
            };
        }
    }
}