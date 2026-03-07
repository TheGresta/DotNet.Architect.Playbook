using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction.Exceptions;

/// <summary>
/// Represents a domain error occurring when a requested resource or entity cannot be located.
/// Inherits from <see cref="DomainException"/> with a <see cref="ErrorCodes.NotFound"/> classification.
/// </summary>
/// <param name="resourceName">The name of the entity type (e.g., "Order", "User").</param>
/// <param name="key">The unique identifier that was used for the lookup.</param>
public sealed class NotFoundException(string resourceName, object key)
    : DomainException(ErrorCodes.NotFound)
{
    /// <summary>
    /// Gets the name of the resource that was not found.
    /// </summary>
    public string ResourceName { get; } = resourceName;

    /// <summary>
    /// Gets the key or identifier associated with the failed lookup.
    /// </summary>
    public object Key { get; } = key;

    /// <summary>
    /// Dispatches the exception to the appropriate mapping logic using the Visitor pattern.
    /// </summary>
    /// <param name="mapper">The implementation of <see cref="IExceptionMapper"/>.</param>
    /// <returns>The result of the mapping operation.</returns>
    public override ExceptionMappingResult Map(IExceptionMapper mapper)
        => mapper.MapSpecific(this);
}
