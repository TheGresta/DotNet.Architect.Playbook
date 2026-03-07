namespace Playbook.Exceptions.Abstraction.Exceptions;

/// <summary>
/// Encapsulates details regarding a specific validation failure.
/// </summary>
/// <param name="Message">The human-readable description of the validation failure.</param>
/// <param name="AttemptedValue">The actual value that triggered the validation failure, if applicable.</param>
public record ValidationError(string Message, object? AttemptedValue);
