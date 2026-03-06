using Microsoft.AspNetCore.Mvc;

namespace Playbook.Exceptions.Models;

public sealed class ApiErrorResponse : ValidationProblemDetails
{
    public string? ErrorCode { get; init; }
    public string? TraceId { get; init; }
    public DebugDetails? Debug { get; init; }
}

public sealed record DebugDetails(string Message, string? StackTrace, DebugDetails? InnerError);