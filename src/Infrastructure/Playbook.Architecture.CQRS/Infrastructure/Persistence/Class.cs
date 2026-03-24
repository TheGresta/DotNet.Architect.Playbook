using System.Collections.Concurrent;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Infrastructure.Persistence;

public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _products.TryGetValue(id, out var product);

        return Task.FromResult(product);
    }
    public Task AddAsync(Product product, CancellationToken ct)
    {
        _products.TryAdd(product.Id, product);

        return Task.CompletedTask;
    }
}
