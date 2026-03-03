using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Playbook.Persistence.EntityFramework.Persistence.Encryption;

/// <summary>
/// Represents a custom value converter for encrypting and decrypting string values using AES encryption.
/// </summary>
/// <remarks>
/// Initializes a new instance of the AesEncryptedConverter class with the specified AES encryption service and optional mapping hints.
/// </remarks>
/// <param name="encryptionService">The service used for AES encryption.</param>
/// <param name="mappingHints">Optional mapping hints for the converter.</param>
internal class AesEncryptedConverter(IAesEncryptionService encryptionService, ConverterMappingHints? mappingHints = default) : ValueConverter<string, string>(
        v => encryptionService.Encrypt(v),  // Converts the input string value to an encrypted string
        v => encryptionService.Decrypt(v),  // Converts the input encrypted string value to its decrypted form
        mappingHints)
{
}
