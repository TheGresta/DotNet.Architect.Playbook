using System.Collections.Concurrent;
using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis;
public sealed class InMemoryProductRepository(ILogger<InMemoryProductRepository> logger) : IProductRepository
{
    // High-performance thread-safe storage with O(1) lookup
    private readonly ConcurrentDictionary<int, ProductDto> _products = new(new Dictionary<int, ProductDto>
    {
        [1] = new(1, "Laptop", 1200.00m),
        [2] = new(2, "Mouse", 25.50m),
        [3] = new(3, "Keyboard", 75.00m)
    });

    // Atomic counter for ID generation
    private int _nextId = 4;

    public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        _products.TryGetValue(id, out var product);

        logger.LogInformation("Repository: GetById({Id}) called. Found: {Found}", id, product != null);

        return Task.FromResult(product);
    }

    public Task<List<ProductDto>> GetAllAsync(CancellationToken ct)
    {
        logger.LogInformation("Repository: GetAll() called.");

        // ConcurrentDictionary.Values is thread-safe, but we return a snapshot via Collection Expression

        var result = _products.Values.ToList();
        return Task.FromResult(result);
    }

    public Task<ProductDto> CreateAsync(ProductDto product, CancellationToken ct)
    {
        // Atomically increment ID to avoid collisions without locking the whole list
        var id = Interlocked.Increment(ref _nextId) - 1;
        var newProduct = product with { Id = id };

        _products.TryAdd(id, newProduct);

        logger.LogInformation("Repository: Created product {Id}: {Name}", id, newProduct.Name);

        return Task.FromResult(newProduct);
    }

    public Task<ProductDto?> UpdateAsync(ProductDto product, CancellationToken ct)
    {
        // TryUpdate ensures we only update if the product actually exists
        if (!_products.ContainsKey(product.Id))
        {
            return Task.FromResult<ProductDto?>(null);
        }

        _products[product.Id] = product;

        logger.LogInformation("Repository: Updated product {Id}", product.Id);

        return Task.FromResult<ProductDto?>(product);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var removed = _products.TryRemove(id, out _);

        if (removed)
        {
            logger.LogInformation("Repository: Deleted product {Id}", id);
        }

        return Task.FromResult(removed);
    }
}