namespace Playbook.Persistence.EntityFramework.Persistence.Encryption;

/// <summary>
/// Defines methods for AES encryption and decryption of string values.
/// </summary>
internal interface IAesEncryptionService
{
    /// <summary>
    /// Encrypts the specified plain text using AES encryption.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted string.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts the specified cipher text using AES decryption.
    /// </summary>
    /// <param name="cipherText">The cipher text to decrypt.</param>
    /// <returns>The decrypted string.</returns>
    string Decrypt(string cipherText);
}
