using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
}
