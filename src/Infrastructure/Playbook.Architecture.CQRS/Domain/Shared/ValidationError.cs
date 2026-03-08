namespace Playbook.Architecture.CQRS.Domain.Shared;

public record ValidationError(string Message, object? AttemptedValue);
