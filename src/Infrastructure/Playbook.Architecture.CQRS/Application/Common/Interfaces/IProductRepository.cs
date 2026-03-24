using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Application.Common.Interfaces;

/// <summary>
/// Defines the architectural contract for persistence operations related to the <see cref="Product"/> aggregate.
/// This interface abstracts the underlying data access technology (e.g., Entity Framework Core, Dapper, or MongoDB)
/// to ensure the domain and application layers remain decoupled from infrastructure concerns.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Asynchronously retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">The unique <see cref="Guid"/> of the product to locate.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the database operation to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains the <see cref="Product"/> if found; otherwise, <see langword="null"/>.
    /// </returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Asynchronously persists a new product entity to the underlying data store.
    /// </summary>
    /// <param name="product">The <see cref="Product"/> instance to be added to the repository.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the database operation to complete.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task AddAsync(Product product, CancellationToken ct);
}
