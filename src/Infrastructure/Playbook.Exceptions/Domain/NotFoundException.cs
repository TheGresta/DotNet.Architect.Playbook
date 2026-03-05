using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Domain;

public sealed class NotFoundException(string resourceName, object key)
    : DomainException($"{resourceName} with ID '{key}' was not found.", ErrorCodes.NotFound)
{
    public string ResourceName { get; } = resourceName;
    public object Key { get; } = key;
}
