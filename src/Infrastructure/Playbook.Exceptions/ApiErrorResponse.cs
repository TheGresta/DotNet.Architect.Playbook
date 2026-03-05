using Microsoft.AspNetCore.Mvc;

namespace Playbook.Exceptions;

public sealed class ApiErrorResponse : ProblemDetails
{
    public string? ErrorCode { get; init; }

    public IDictionary<string, string[]>? Errors { get; init; }
}