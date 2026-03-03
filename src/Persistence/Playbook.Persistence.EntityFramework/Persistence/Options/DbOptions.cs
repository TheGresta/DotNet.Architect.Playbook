namespace Playbook.Persistence.EntityFramework.Persistence.Options;


/// <summary>
/// Represents the configuration options for the database connection.
/// </summary>
public class DbOptions
{
    /// <summary>
    /// Gets or sets the name of the database.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the application.
    /// </summary>
    /// <remarks>
    /// This property is used as the default value for the 'CreatedBy' field in auditable entities.
    /// </remarks>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string for the database.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether encryption is enabled for the database.
    /// </summary>
    public bool EncryptionEnabled { get; set; } = false;
}
