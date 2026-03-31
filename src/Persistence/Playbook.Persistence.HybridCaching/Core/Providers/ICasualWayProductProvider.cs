using Playbook.Persistence.HybridCaching.Core.Entities;

namespace Playbook.Persistence.HybridCaching.Core.Providers;

/// <summary>
/// Defines the service contract for retrieving and managing the cache state of <see cref="CasualWayProduct"/> entities.
/// </summary>
public interface ICasualWayProductProvider
{
    /// <summary>
    /// Retrieves a specific product by its identifier, utilizing the cache-aside pattern.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the product entity.</returns>
    Task<CasualWayProduct> GetProductAsync(int id, CancellationToken ct);

    /// <summary>
    /// Retrieves a collection of all available products, potentially fetching a high-volume payload.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of products.</returns>
    Task<List<CasualWayProduct>> GetProductsAsync(CancellationToken ct);

    /// <summary>
    /// Explicitly triggers an invalidation of the product list cache to force a refresh on the next request.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous invalidation.</returns>
    Task ReloadProductsAsync(CancellationToken ct);
}
