using System.Collections.Concurrent;

using ErrorOr;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Domain.Common;
using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Infrastructure.Persistence;

public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public async Task<ErrorOr<Product>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await Task.Delay(50, ct); // Simulate I/O
        return _products.TryGetValue(id, out var product)
            ? product
            : DomainErrors.Product.NotFound;
    }

    public async Task<ErrorOr<Success>> AddAsync(Product product, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        return _products.TryAdd(product.Id, product)
            ? Result.Success
            : Error.Conflict("Product.AlreadyExists", "A product with this ID already exists.");
    }

    public async Task<ErrorOr<List<Product>>> ListAsync(CancellationToken ct)
    {
        await Task.Delay(50, ct);
        return _products.Values.ToList();
    }
}
