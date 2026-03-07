using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public sealed class ValidationException(IReadOnlyDictionary<string, ValidationError[]> errors)
    : DomainException(ErrorCodes.ValidationError)
{
    public IReadOnlyDictionary<string, ValidationError[]> Errors { get; } = errors;
    public override ExceptionMappingResult Map(IExceptionMapper mapper)
        => mapper.MapSpecific(this);
}
