using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis;

public class InMemoryProductRepository : IProductRepository
{
    private readonly List<ProductDto> _products = new()
{
    new ProductDto(1, "Laptop", 1200.00m),
    new ProductDto(2, "Mouse", 25.50m),
    new ProductDto(3, "Keyboard", 75.00m)
};
    private readonly ILogger<InMemoryProductRepository> _logger;
    private readonly object _lock = new();

    public InMemoryProductRepository(ILogger<InMemoryProductRepository> logger)
    {
        _logger = logger;
    }

    public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        lock (_lock)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            _logger.LogInformation("Repository: GetById({Id}) called. Found: {Found}", id, product != null);
            return Task.FromResult(product);
        }
    }

    public Task<List<ProductDto>> GetAllAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            _logger.LogInformation("Repository: GetAll() called.");
            return Task.FromResult(_products.ToList());
        }
    }

    public Task<ProductDto> CreateAsync(ProductDto product, CancellationToken ct)
    {
        lock (_lock)
        {
            var newId = _products.Count > 0 ? _products.Max(p => p.Id) + 1 : 1;
            var newProduct = product with { Id = newId };
            _products.Add(newProduct);
            _logger.LogInformation("Repository: Created product {Id}: {Name}", newProduct.Id, newProduct.Name);
            return Task.FromResult(newProduct);
        }
    }

    public Task<ProductDto?> UpdateAsync(ProductDto product, CancellationToken ct)
    {
        lock (_lock)
        {
            var existing = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existing == null) return Task.FromResult<ProductDto?>(null);

            _products.Remove(existing);
            _products.Add(product);
            _logger.LogInformation("Repository: Updated product {Id}", product.Id);
            return Task.FromResult<ProductDto?>(product);
        }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        lock (_lock)
        {
            var existing = _products.FirstOrDefault(p => p.Id == id);
            if (existing == null) return Task.FromResult(false);

            _products.Remove(existing);
            _logger.LogInformation("Repository: Deleted product {Id}", id);
            return Task.FromResult(true);
        }
    }
}
