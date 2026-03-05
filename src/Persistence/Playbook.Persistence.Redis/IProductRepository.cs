using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis;

public interface IProductRepository
{
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<ProductDto>> GetAllAsync(CancellationToken ct);
    Task<ProductDto> CreateAsync(ProductDto product, CancellationToken ct);
    Task<ProductDto?> UpdateAsync(ProductDto product, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
