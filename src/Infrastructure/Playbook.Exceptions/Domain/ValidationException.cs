using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Domain;

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : DomainException("One or more validation failures occurred.", ErrorCodes.ValidationError)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}