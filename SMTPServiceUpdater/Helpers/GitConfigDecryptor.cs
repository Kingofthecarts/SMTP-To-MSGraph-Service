using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMTPServiceUpdater.Helpers
{
    /// <summary>
    /// Handles decryption of encrypted GitHub configuration values.
    /// Uses the same encryption key as the main SMTP Service.
    /// </summary>
    public static class GitConfigDecryptor
    {
        // Must match the encryption key in SMTP Service
        private const string ENCRYPTION_KEY = "SMTP2GraphRelay2025SecureKey!32c";
        private const string SALT = "SMTPRelay2025";
        private const int ITERATIONS = 1000;

        /// <summary>
        /// Decrypts an encrypted string using AES encryption.
        /// </summary>
        /// <param name="encryptedText">The base64-encoded encrypted text</param>
        /// <returns>Decrypted string, or empty string if decryption fails</returns>
        public static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                return string.Empty;
            }

            try
            {
                var saltBytes = Encoding.UTF8.GetBytes(SALT);

                using var passwordDerive = new Rfc2898DeriveBytes(
                    ENCRYPTION_KEY, 
                    saltBytes, 
                    ITERATIONS, 
                    HashAlgorithmName.SHA256);
                
                var keyBytes = passwordDerive.GetBytes(32); // 256-bit
                var ivBytes = passwordDerive.GetBytes(16);  // 128-bit

                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var encryptedBytes = Convert.FromBase64String(encryptedText);

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                return reader.ReadToEnd();
            }
            catch (FormatException)
            {
                // Invalid base64 string - might be unencrypted for testing
                return string.Empty;
            }
            catch (CryptographicException)
            {
                // Decryption failed - wrong key or corrupted data
                return string.Empty;
            }
            catch (Exception)
            {
                // Any other error
                return string.Empty;
            }
        }
    }
}
