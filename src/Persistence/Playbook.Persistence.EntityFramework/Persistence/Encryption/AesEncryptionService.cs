using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Playbook.Persistence.EntityFramework.Persistence.Options;

namespace Playbook.Persistence.EntityFramework.Persistence.Encryption;

internal class AesEncryptionService(IOptions<EncryptionOptions> encryptionOptions, IOptions<DbOptions> dbOptions) : IAesEncryptionService
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(encryptionOptions.Value.Key);
    private readonly bool _encryptionEnabled = dbOptions.Value.EncryptionEnabled;

    public string Encrypt(string plainText)
    {
        if (!_encryptionEnabled || string.IsNullOrWhiteSpace(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        // Generate a NEW random IV for every single encryption
        aes.GenerateIV();

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // Store the IV at the beginning of the stream
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (!_encryptionEnabled || string.IsNullOrWhiteSpace(cipherText)) return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        // Extract the IV from the first 16 bytes
        var iv = new byte[aes.BlockSize / 8];
        var cipherTextBytes = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherTextBytes, 0, cipherTextBytes.Length);

        aes.IV = iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipherTextBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
