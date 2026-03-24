using System.Collections.Concurrent;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Infrastructure.Persistence;

/// <summary>
/// A high-performance, thread-safe in-memory implementation of the <see cref="IProductRepository"/>.
/// This implementation utilizes a <see cref="ConcurrentDictionary{TKey, TValue}"/> to facilitate 
/// non-blocking read and write operations, making it ideal for unit testing, prototyping, 
/// or low-latency transient data storage.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    /// <summary>
    /// The internal backing store for products. 
    /// ConcurrentDictionary is used to ensure thread safety across parallel MediatR request executions 
    /// without requiring manual lock management.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    /// <summary>
    /// Retrieves a product from the in-memory store by its unique identifier.
    /// </summary>
    /// <param name="id">The unique <see cref="Guid"/> of the product.</param>
    /// <param name="ct">The cancellation token (ignored in this synchronous in-memory implementation).</param>
    /// <returns>A completed task containing the <see cref="Product"/> if present; otherwise, <see langword="null"/>.</returns>
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // TryGetValue provides an atomic, thread-safe way to check for existence and retrieve the value.
        _products.TryGetValue(id, out var product);

        return Task.FromResult(product);
    }

    /// <summary>
    /// Persists a new product instance to the in-memory dictionary.
    /// </summary>
    /// <param name="product">The product aggregate to store.</param>
    /// <param name="ct">The cancellation token (ignored in this synchronous in-memory implementation).</param>
    /// <returns>A <see cref="Task.CompletedTask"/> representing the successful completion of the operation.</returns>
    public Task AddAsync(Product product, CancellationToken ct)
    {
        // TryAdd prevents overwriting existing entries, maintaining the integrity of the initial persistence.
        _products.TryAdd(product.Id, product);

        return Task.CompletedTask;
    }
}
