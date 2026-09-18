namespace API_asemp.Seguridad
{
    /// <summary>
    /// Utilidad para cifrar y verificar contraseñas usando BCrypt.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Genera un hash seguro a partir de una contraseña en texto plano.
        /// </summary>
        public static string Hash(string plain)
        {
            return BCrypt.Net.BCrypt.HashPassword(plain, workFactor: 11);
        }

        /// <summary>
        /// Verifica si una contraseña en texto plano coincide con el hash almacenado.
        /// </summary>
        public static bool Verify(string plain, string hash)
        {
            if (string.IsNullOrWhiteSpace(hash)) return false;
            return BCrypt.Net.BCrypt.Verify(plain, hash);
        }

        /// <summary>
        /// Determina si una cadena ya está hasheada con BCrypt.
        /// </summary>
        public static bool IsHashed(string? value)
        {
            return value?.StartsWith("$2") == true;
        }
    }
}
