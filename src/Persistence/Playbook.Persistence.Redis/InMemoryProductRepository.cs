using System.Collections.Concurrent;
using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis;
/// <summary>
/// Provides a high-performance, thread-safe in-memory implementation of <see cref="IProductRepository"/> for testing and prototyping.
/// </summary>
/// <remarks>
/// This implementation uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> for <c>O(1)</c> lookups and 
/// <see cref="Interlocked.Increment(ref int)"/> for atomic ID generation, ensuring thread safety without global locks.
/// </remarks>
public sealed class InMemoryProductRepository(ILogger<InMemoryProductRepository> logger) : IProductRepository
{
    private readonly ConcurrentDictionary<int, ProductDto> _products = new(new Dictionary<int, ProductDto>
    {
        [1] = new(1, "Laptop", 1200.00m),
        [2] = new(2, "Mouse", 25.50m),
        [3] = new(3, "Keyboard", 75.00m)
    });

    private int _nextId = 4;

    /// <inheritdoc />
    public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        _products.TryGetValue(id, out var product);
        logger.LogInformation("Repository: GetById({Id}) called. Found: {Found}", id, product != null);
        return Task.FromResult(product);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns a snapshot of the current values. Note that the order of the list is not guaranteed due to 
    /// the nature of <see cref="ConcurrentDictionary{TKey, TValue}"/>.
    /// </remarks>
    public Task<List<ProductDto>> GetAllAsync(CancellationToken ct)
    {
        logger.LogInformation("Repository: GetAll() called.");
        var result = _products.Values.ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="Interlocked"/> to generate a unique identifier in a thread-safe manner before adding to the collection.
    /// </remarks>
    public Task<ProductDto> CreateAsync(ProductDto product, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _nextId) - 1;
        var newProduct = product with { Id = id };

        _products.TryAdd(id, newProduct);
        logger.LogInformation("Repository: Created product {Id}: {Name}", id, newProduct.Name);

        return Task.FromResult(newProduct);
    }

    /// <inheritdoc />
    public Task<ProductDto?> UpdateAsync(ProductDto product, CancellationToken ct)
    {
        if (!_products.ContainsKey(product.Id))
        {
            return Task.FromResult<ProductDto?>(null);
        }

        _products[product.Id] = product;
        logger.LogInformation("Repository: Updated product {Id}", product.Id);

        return Task.FromResult<ProductDto?>(product);
    }

    /// <inheritdoc />
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