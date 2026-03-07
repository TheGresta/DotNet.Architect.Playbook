namespace Playbook.Exceptions.Core;

/// <summary>
/// Encapsulates the result of an exception mapping operation, formatted for API responses.
/// This structure is compatible with RFC 7807 (Problem Details for HTTP APIs).
/// </summary>
/// <param name="Title">A short, human-readable summary of the error type.</param>
/// <param name="Detail">A detailed, localized explanation specific to this occurrence of the error.</param>
/// <param name="Code">A machine-readable application error code.</param>
/// <param name="StatusCode">The associated HTTP status code.</param>
/// <param name="Extensions">Optional additional metadata, such as validation failure details.</param>
public record ExceptionMappingResult(string Title, string Detail, string Code, int StatusCode, IDictionary<string, string[]>? Extensions = null);
