using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

public static class CryptoHelper
{
    public static byte[] GenerateSalt(int size = 16)
    {
        byte[] salt = new byte[size];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return salt;
    }

    public static byte[] DeriveKey(string password, byte[] salt, int keySize = 32)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 8,  // number of CPU cores to use
            MemorySize = 65536,       // 64 MB memory usage
            Iterations = 4            // number of iterations
        };
        return argon2.GetBytes(keySize);
    }

    // For password hashing, we just use the key derivation function.
    public static byte[] HashPassword(string password, byte[] salt)
    {
        return DeriveKey(password, salt);
    }

    public static byte[] EncryptBytes(byte[] plainData, byte[] key, out byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            iv = aes.IV;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainData, 0, plainData.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }

    public static byte[] DecryptBytes(byte[] cipherData, byte[] key, byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherData, 0, cipherData.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }
}
