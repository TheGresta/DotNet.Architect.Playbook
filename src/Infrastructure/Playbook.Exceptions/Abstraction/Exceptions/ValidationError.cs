namespace Playbook.Exceptions.Abstraction.Exceptions;

public record ValidationError(string Message, object? AttemptedValue);