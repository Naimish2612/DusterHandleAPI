using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Auth
{
    public static class PasswordUtility
    {
        private const int SaltSize = 32;        // 256 bit
        private const int HashSize = 64;        // 512 bit
        private const int Iterations = 100000;  // Strong security

        public static (string Hash, string Salt) CreatePasswordHash(string password)
        {
            // Generate Salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Generate Hash
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);

            byte[] hash = pbkdf2.GetBytes(HashSize);

            return (
                Convert.ToBase64String(hash),
                Convert.ToBase64String(salt)
            );
        }


        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA512);

            byte[] computedHash = pbkdf2.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(
                computedHash,
                storedHashBytes
            );
        }
    }
}
