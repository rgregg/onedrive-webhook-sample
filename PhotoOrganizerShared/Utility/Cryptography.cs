using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PhotoOrganizerShared.Utility
{
    /// <summary>
    /// Basic encryption / decryption support. Copied from http://stackoverflow.com/questions/273452/using-aes-encryption-in-c-sharp
    /// </summary>
    public static class Cryptography
    {
        #region Settings

        private static int _iterations = 2;
        private static int _keySize = 256;

        private static string _hash = "SHA1";
        private static string _salt = "Pbe5JE580DfN2bpg"; // Random 16 ASCII characters
        private static string _vector = "uW7PWgTQzVnk2U5g"; // Random 16 ASCII characters

        #endregion

        public static string Encrypt(this string value, string password)
        {
            return Encrypt<AesManaged>(value, password);
        }
        public static string Encrypt<T>(this string value, string password)
                where T : SymmetricAlgorithm, new()
        {
            byte[] vectorBytes = GetBytes<ASCIIEncoding>(_vector);
            byte[] saltBytes = GetBytes<ASCIIEncoding>(_salt);
            byte[] valueBytes = GetBytes<UTF8Encoding>(value);

            byte[] encrypted;
            using (T cipher = new T())
            {
                PasswordDeriveBytes _passwordBytes =
                    new PasswordDeriveBytes(password, saltBytes, _hash, _iterations);
                byte[] keyBytes = _passwordBytes.GetBytes(_keySize / 8);

                cipher.Mode = CipherMode.CBC;

                using (ICryptoTransform encryptor = cipher.CreateEncryptor(keyBytes, vectorBytes))
                {
                    using (MemoryStream to = new MemoryStream())
                    {
                        using (CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write))
                        {
                            writer.Write(valueBytes, 0, valueBytes.Length);
                            writer.FlushFinalBlock();
                            encrypted = to.ToArray();
                        }
                    }
                }
                cipher.Clear();
            }
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(this string value, string password)
        {
            return Decrypt<AesManaged>(value, password);
        }
        public static string Decrypt<T>(this string value, string password) where T : SymmetricAlgorithm, new()
        {
            byte[] vectorBytes = GetBytes<ASCIIEncoding>(_vector);
            byte[] saltBytes = GetBytes<ASCIIEncoding>(_salt);
            byte[] valueBytes = Convert.FromBase64String(value);

            byte[] decrypted;
            int decryptedByteCount = 0;

            using (T cipher = new T())
            {
                PasswordDeriveBytes _passwordBytes = new PasswordDeriveBytes(password, saltBytes, _hash, _iterations);
                byte[] keyBytes = _passwordBytes.GetBytes(_keySize / 8);

                cipher.Mode = CipherMode.CBC;

                try
                {
                    using (ICryptoTransform decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes))
                    {
                        using (MemoryStream from = new MemoryStream(valueBytes))
                        {
                            using (CryptoStream reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
                            {
                                decrypted = new byte[valueBytes.Length];
                                decryptedByteCount = reader.Read(decrypted, 0, decrypted.Length);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return String.Empty;
                }

                cipher.Clear();
            }
            return Encoding.UTF8.GetString(decrypted, 0, decryptedByteCount);
        }

        private static byte[] GetBytes<T>(string value) where T : System.Text.Encoding, new()
        {
            var encoding = new T();
            return encoding.GetBytes(value);
        }
    }
}
