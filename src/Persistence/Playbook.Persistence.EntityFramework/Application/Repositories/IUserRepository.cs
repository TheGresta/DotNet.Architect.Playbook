using Playbook.Persistence.EntityFramework.Domain;

namespace Playbook.Persistence.EntityFramework.Application.Repositories;

/// <summary>
/// Defines the data access contract specialized for <see cref="UserEntity"/> operations.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IBaseRepository{UserEntity}"/>, inheriting standard 
/// CRUD, projection, and pagination capabilities specifically for user data. 
/// Add custom user-specific queries (e.g., searching by email or role) to this contract.
/// </remarks>
public interface IUserRepository : IBaseRepository<UserEntity>
{
    // Custom domain-specific methods can be defined here, for example:
    // Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}