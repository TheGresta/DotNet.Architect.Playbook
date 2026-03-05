namespace Playbook.Exceptions.Domain;

public sealed class NotFoundException(string resourceName, object key)
    : DomainException($"{resourceName} with ID '{key}' was not found.", "RESOURCE_NOT_FOUND")
{
    public string ResourceName { get; } = resourceName;
    public object Key { get; } = key;
}
