namespace Playbook.Persistence.ElasticSearch.Application.Models;

/// <summary>
/// Represents the outcome of a search infrastructure operation.
/// </summary>
/// <param name="IsSuccess">Indicates whether the operation completed successfully.</param>
/// <param name="ErrorMessage">Contains the descriptive error message if the operation failed.</param>
/// <param name="ErrorCode">An optional machine-readable error code for programmatic handling.</param>
public readonly record struct SearchOperationResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    string? ErrorCode = null)
{
    /// <summary>
    /// Creates a successful <see cref="SearchOperationResult"/>.
    /// </summary>
    /// <returns>A <see cref="SearchOperationResult"/> with <see cref="IsSuccess"/> set to <c>true</c>.</returns>
    public static SearchOperationResult Success() => new(true);

    /// <summary>
    /// Creates a failed <see cref="SearchOperationResult"/> with specific error details.
    /// </summary>
    /// <param name="error">The message describing the failure.</param>
    /// <param name="code">An optional unique code representing the error type.</param>
    /// <returns>A <see cref="SearchOperationResult"/> with <see cref="IsSuccess"/> set to <c>false</c>.</returns>
    public static SearchOperationResult Failure(string error, string? code = null)
        => new(false, error, code);
}