using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public sealed class ValidationException(IReadOnlyDictionary<string, ValidationError[]> errors)
    : DomainException(ErrorCodes.ValidationError)
{
    public IReadOnlyDictionary<string, ValidationError[]> Errors { get; } = errors;
}
