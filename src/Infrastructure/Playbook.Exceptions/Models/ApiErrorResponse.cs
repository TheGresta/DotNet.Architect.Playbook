using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

namespace Playbook.Exceptions.Models;

/// <summary>
/// A specialized extension of <see cref="ValidationProblemDetails"/> that aligns with RFC 7807 
/// while providing additional diagnostic and application-specific metadata.
/// This model serves as the unified error contract for all API consumers.
/// </summary>
public sealed class ApiErrorResponse : ValidationProblemDetails
{
    /// <summary>
    /// Gets a machine-readable application error code used for programmatic handling by clients.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets the unique trace identifier for the request, facilitating log correlation 
    /// across distributed systems.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Gets detailed diagnostic information, including stack traces and inner exceptions.
    /// This property is only populated in development environments and is omitted from 
    /// production responses to prevent information disclosure.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("debug")]
    public DebugDetails? Debug { get; init; }
}

/// <summary>
/// Encapsulates technical diagnostic data for an exception, designed for recursive 
/// representation of exception hierarchies.
/// </summary>
/// <param name="Message">The direct exception message.</param>
/// <param name="StackTrace">The immediate stack trace associated with the error.</param>
/// <param name="InnerError">The captured debug details of the underlying inner exception, if one exists.</param>
public sealed record DebugDetails(
    string Message,
    string? StackTrace,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DebugDetails? InnerError);
