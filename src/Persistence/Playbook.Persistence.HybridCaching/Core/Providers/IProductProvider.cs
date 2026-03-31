using Playbook.Persistence.HybridCaching.Core.Entities;

namespace Playbook.Persistence.HybridCaching.Core.Providers;

/// <summary>
/// Defines the service contract for retrieving and managing the cache state of optimized <see cref="Product"/> entities.
/// </summary>
public interface IProductProvider
{
    /// <summary>
    /// Retrieves a specific optimized product by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the product entity.</returns>
    Task<Product> GetProductAsync(int id, CancellationToken ct);

    /// <summary>
    /// Retrieves a collection of all optimized products.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of products.</returns>
    Task<List<Product>> GetProductsAsync(CancellationToken ct);

    /// <summary>
    /// Triggers a cache invalidation for the optimized product list.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous invalidation.</returns>
    Task ReloadProductsAsync(CancellationToken ct);
}
