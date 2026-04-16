namespace Playbook.Security.IdP.Application.Abstractions.Common;

/// <summary>
/// A structural contract for DTOs or Entities that possess a unique identifier.
/// This version is "Gold Standard" because it supports Strongly Typed IDs 
/// instead of forcing a raw Guid.
/// </summary>
public interface IIdentifiable<out TId>
{
    /// <summary>
    /// Gets the strongly-typed identifier.
    /// </summary>
    TId Id { get; }
}
