using ErrorOr;

using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<ErrorOr<Product>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ErrorOr<Success>> AddAsync(Product product, CancellationToken ct);
    Task<ErrorOr<List<Product>>> ListAsync(CancellationToken ct);
}
