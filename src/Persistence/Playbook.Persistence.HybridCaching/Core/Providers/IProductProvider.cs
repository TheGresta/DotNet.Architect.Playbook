using Playbook.Persistence.HybridCaching.Core.Entities;

namespace Playbook.Persistence.HybridCaching.Core.Providers;

public interface IProductProvider
{
    Task<Product> GetProductAsync(int id, CancellationToken ct);
    Task<List<Product>> GetProductsAsync(CancellationToken ct);
    Task ReloadProductsAsync(CancellationToken ct);
}
