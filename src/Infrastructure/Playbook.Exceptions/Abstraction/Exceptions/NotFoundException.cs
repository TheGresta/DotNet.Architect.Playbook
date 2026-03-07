using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public sealed class NotFoundException(string resourceName, object key)
    : DomainException(ErrorCodes.NotFound)
{
    public string ResourceName { get; } = resourceName;
    public object Key { get; } = key;
    public override ExceptionMappingResult Map(IExceptionMapper mapper)
        => mapper.MapSpecific(this);
}
