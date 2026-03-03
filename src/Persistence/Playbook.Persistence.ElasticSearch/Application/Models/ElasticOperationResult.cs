namespace Playbook.Persistence.ElasticSearch.Application.Models;

public record ElasticOperationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public int? StatusCode { get; init; }

    public static ElasticOperationResult Success() => new() { IsSuccess = true };
    public static ElasticOperationResult Failure(string error, int? code = null)
        => new() { IsSuccess = false, ErrorMessage = error, StatusCode = code };
}
