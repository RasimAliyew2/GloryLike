using System;
using System.Security.Cryptography;

namespace GloryLikeBackend.Services.Hash
{
    

    namespace GloryLikeBackend.Helpers
    {
        public static class PasswordHasher
        {
            private const int SaltSize = 16;   // 128 bit
            private const int KeySize = 32;    // 256 bit
            private const int Iterations = 100_000;
            private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

            public static string HashPassword(string password)
            {
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password boş ola bilməz.", nameof(password));

                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

                byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Iterations,
                    Algorithm,
                    KeySize);

                return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
            }

            public static bool VerifyPassword(string password, string storedHash)
            {
                if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                    return false;

                string[] parts = storedHash.Split('.', 3);
                if (parts.Length != 3)
                    return false;

                if (!int.TryParse(parts[0], out int iterations))
                    return false;

                byte[] salt;
                byte[] hash;

                try
                {
                    salt = Convert.FromBase64String(parts[1]);
                    hash = Convert.FromBase64String(parts[2]);
                }
                catch
                {
                    return false;
                }

                byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    Algorithm,
                    hash.Length);

                return CryptographicOperations.FixedTimeEquals(inputHash, hash);
            }
        }
    }
}
