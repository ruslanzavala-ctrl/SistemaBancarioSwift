using System.Text.RegularExpressions;

namespace SistemaBancario.Security
{
    public static class PasswordValidator
    {
        // Expresión regular: Mínimo 8 caracteres, al menos una mayúscula, una minúscula, un número y un símbolo especial
        private static readonly string PolicyPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$";

        public static bool ValidatePolicy(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            return Regex.IsMatch(password, PolicyPattern);
        }
    }
}