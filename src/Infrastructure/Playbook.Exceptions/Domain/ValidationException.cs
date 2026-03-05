namespace Playbook.Exceptions.Domain;

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : DomainException("One or more validation failures occurred.", "VALIDATION_ERROR")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}