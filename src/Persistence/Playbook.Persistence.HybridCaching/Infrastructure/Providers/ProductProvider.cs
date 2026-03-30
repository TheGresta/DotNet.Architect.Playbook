using Playbook.Persistence.HybridCaching.Core.Entities;
using Playbook.Persistence.HybridCaching.Core.Interfaces;
using Playbook.Persistence.HybridCaching.Core.Providers;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Providers;

public sealed class ProductProvider(ICacheProvider cacheProvider) : IProductProvider
{
    public async Task<Product> GetProductAsync(int id, CancellationToken ct) =>
        await cacheProvider.GetOrAddAsync(cancel => FetchProductFromDb(id, cancel), id.ToString(), ct);

    public async Task<List<Product>> GetProductsAsync(CancellationToken ct) =>
        await cacheProvider.GetOrAddAsync(FetchAllFromDb, "all", ct);

    public async Task ReloadProductsAsync(CancellationToken ct) => await cacheProvider.NotifyInvalidationAsync<List<CasualWayProduct>>(ct);

    // Simulated DB Calls
    private static ValueTask<Product> FetchProductFromDb(int id, CancellationToken ct) =>
        new(new Product { Id = id, Name = $"Smart Item {id}", Price = id * 3.17M });

    private static ValueTask<List<Product>> FetchAllFromDb(CancellationToken ct)
    {
        var products = Enumerable.Range(1, 100_000).Select(i => new Product { Id = i, Name = $"Smart Item {i}", Price = i * 3.17M }).ToList();

        return ValueTask.FromResult(products);
    }
}
