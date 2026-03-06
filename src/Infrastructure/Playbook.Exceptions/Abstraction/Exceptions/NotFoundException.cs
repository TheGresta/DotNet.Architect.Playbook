using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public sealed class NotFoundException(string resourceName, object key)
    : DomainException(ErrorCodes.NotFound)
{
    public string ResourceName { get; } = resourceName;
    public object Key { get; } = key;
}