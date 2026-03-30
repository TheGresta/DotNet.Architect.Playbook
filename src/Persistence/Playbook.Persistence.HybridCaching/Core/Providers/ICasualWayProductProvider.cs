using Playbook.Persistence.HybridCaching.Core.Entities;

namespace Playbook.Persistence.HybridCaching.Core.Providers;

public interface ICasualWayProductProvider
{
    Task<CasualWayProduct> GetProductAsync(int id, CancellationToken ct);
    Task<List<CasualWayProduct>> GetProductsAsync(CancellationToken ct);
    Task ReloadProductsAsync(CancellationToken ct);
}
