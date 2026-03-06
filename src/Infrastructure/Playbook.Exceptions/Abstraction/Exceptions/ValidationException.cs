using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : DomainException(ErrorCodes.ValidationError)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}