using System;
using System.IO;
using System.Security.Cryptography;

namespace eBriefingWebApp.Helper
{
    public class EncryptionHelper
    {
        public static string EncryptString(string password, byte[] _salt)
        {
            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, _salt);
                aesAlg.Key = pdb.GetBytes(32);
                aesAlg.IV = pdb.GetBytes(16);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(password);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        public static byte[] GenerateSalt(int length)
        {
            using (var randomNumberGenerator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[length];
                randomNumberGenerator.GetBytes(randomNumber);
                return randomNumber;
            }
        }

    }
}