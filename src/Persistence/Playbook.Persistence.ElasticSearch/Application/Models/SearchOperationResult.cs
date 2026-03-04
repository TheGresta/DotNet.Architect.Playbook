namespace Playbook.Persistence.ElasticSearch.Application.Models;

public readonly record struct SearchOperationResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    string? ErrorCode = null)
{
    public static SearchOperationResult Success() => new(true);
    public static SearchOperationResult Failure(string error, string? code = null)
        => new(false, error, code);
}
