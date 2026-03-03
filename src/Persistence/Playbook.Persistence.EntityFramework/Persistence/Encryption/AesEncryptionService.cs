using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Playbook.Persistence.EntityFramework.Persistence.Options;

namespace Playbook.Persistence.EntityFramework.Persistence.Encryption;

/// <summary>
/// Provides high-level cryptographic services using the Advanced Encryption Standard (AES).
/// </summary>
/// <remarks>
/// This service implements a "Randomized IV" strategy. Every encryption operation generates 
/// a unique Initialization Vector (IV), which is stored alongside the ciphertext to enable 
/// secure, stateless decryption.
/// </remarks>
internal class AesEncryptionService(IOptions<EncryptionOptions> encryptionOptions, IOptions<DbOptions> dbOptions) : IAesEncryptionService
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(encryptionOptions.Value.Key);
    private readonly bool _encryptionEnabled = dbOptions.Value.EncryptionEnabled;

    /// <summary>
    /// Encrypts a plain-text string into a Base64-encoded ciphertext.
    /// </summary>
    /// <param name="plainText">The sensitive string to be encrypted.</param>
    /// <returns>
    /// If encryption is enabled, returns a Base64 string containing the 16-byte IV followed by 
    /// the encrypted data. Otherwise, returns the original <paramref name="plainText"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Logic:</b>
    /// 1. Verifies if encryption is globally enabled and the input is not empty.<br/>
    /// 2. Generates a cryptographically strong random IV via <see cref="Aes.GenerateIV"/>.<br/>
    /// 3. Writes the IV to the beginning of a <see cref="MemoryStream"/>.<br/>
    /// 4. Appends the encrypted bytes using a <see cref="CryptoStream"/>.<br/>
    /// 5. Converts the combined buffer (IV + Ciphertext) to Base64 for database storage.
    /// </para>
    /// </remarks>
    public string Encrypt(string plainText)
    {
        if (!_encryptionEnabled || string.IsNullOrWhiteSpace(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        // The IV is unique per encryption to ensure semantic security.
        aes.GenerateIV();

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // The IV is needed for decryption, so it is prepended to the stream.
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts a Base64-encoded ciphertext back into plain-text.
    /// </summary>
    /// <param name="cipherText">The Base64 string containing the IV and the encrypted data.</param>
    /// <returns>
    /// The original plain-text string. If encryption is disabled, returns <paramref name="cipherText"/> as is.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Logic:</b>
    /// 1. Decodes the Base64 input into a byte array.<br/>
    /// 2. Slices the first 16 bytes (128 bits) to extract the unique IV.<br/>
    /// 3. Uses the remaining bytes as the actual payload for the decryption algorithm.<br/>
    /// 4. Configures the <see cref="Aes"/> instance with the shared Key and the extracted IV.<br/>
    /// 5. Streams the data through <see cref="CryptoStream"/> to recover the plain text.
    /// </para>
    /// </remarks>
    public string Decrypt(string cipherText)
    {
        if (!_encryptionEnabled || string.IsNullOrWhiteSpace(cipherText)) return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        // AES block size is 128 bits (16 bytes). 
        var iv = new byte[aes.BlockSize / 8];
        var cipherTextBytes = new byte[fullCipher.Length - iv.Length];

        // Separation of the IV from the payload.
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