namespace Playbook.Persistence.EntityFramework.Persistence.Options;

/// <summary>
/// Represents the encryption options for the .NET 8.0 EF Core project.
/// </summary>
public class EncryptionOptions
{
    /// <summary>
    /// Gets or sets the encryption key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}