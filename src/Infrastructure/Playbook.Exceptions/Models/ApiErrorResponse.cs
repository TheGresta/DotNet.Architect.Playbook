using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

namespace Playbook.Exceptions.Models;

public sealed class ApiErrorResponse : ValidationProblemDetails
{
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("debug")]
    public DebugDetails? Debug { get; init; }
}

public sealed record DebugDetails(
    string Message,
    string? StackTrace,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DebugDetails? InnerError);
