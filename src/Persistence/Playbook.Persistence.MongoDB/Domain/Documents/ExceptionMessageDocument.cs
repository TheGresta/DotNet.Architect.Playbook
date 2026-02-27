namespace Playbook.Persistence.MongoDB.Domain.Documents;

/// <summary>
/// Represents a persisted definition of an error message within MongoDB.
/// </summary>
/// <remarks>
/// Inherits from <see cref="BaseDocument"/> to include standard metadata such as <see cref="BaseDocument.Id"/> and <see cref="BaseDocument.CreatedAt"/>.
/// </remarks>
public class ExceptionMessageDocument : BaseDocument
{
    /// <summary>
    /// Gets or sets a unique alphanumeric code representing the specific exception type or business error.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the descriptive text explaining the exception details.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
