namespace Playbook.Architecture.CQRS.Application.Common.Interfaces;

/// <summary>
/// A structural contract for domain entities or data transfer objects that possess a unique identifier.
/// This interface facilitates generic logic for logging, auditing, or cache-key generation based on entity identity.
/// </summary>
public interface IHasId
{
    /// <summary>
    /// Gets the unique <see cref="Guid"/> associated with the object instance.
    /// </summary>
    Guid Id { get; }
}
