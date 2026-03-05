using Microsoft.AspNetCore.Mvc;
using Playbook.Persistence.Redis.Interfaces;
using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ICacheService cache,
        IProductRepository repository,
        ILogger<ProductsController> logger)
    {
        _cache = cache;
        _repository = repository;
        _logger = logger;
    }

    // GET: api/products/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        var product = await _cache.GetOrSetAsync(
            "products",
            id.ToString(),
            async token =>
            {
                // This factory runs only on cache miss
                var p = await _repository.GetByIdAsync(id, token);
                return p ?? throw new KeyNotFoundException($"Product {id} not found");
            },
            expiration: TimeSpan.FromMinutes(5),
            cancellationToken: ct);

        return Ok(product);
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(CancellationToken ct)
    {
        var products = await _cache.GetOrSetAsync(
            "products",
            "all",
            async token =>
            {
                // Cache miss – fetch from repository
                return await _repository.GetAllAsync(token);
            },
            expiration: TimeSpan.FromMinutes(5),
            cancellationToken: ct);

        return Ok(products);
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductDto product, CancellationToken ct)
    {
        var created = await _repository.CreateAsync(product, ct);

        // Invalidate the entire products prefix because the list changed
        await _cache.InvalidatePrefixAsync("products", ct);

        // Optionally also remove the specific key if it existed (but prefix invalidation already does that logically)
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT: api/products/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProductDto product, CancellationToken ct)
    {
        if (id != product.Id)
            return BadRequest("ID mismatch");

        var updated = await _repository.UpdateAsync(product, ct);
        if (updated == null)
            return NotFound();

        // Invalidate the products prefix (list and possibly this product)
        await _cache.InvalidatePrefixAsync("products", ct);

        return NoContent();
    }

    // DELETE: api/products/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        // Invalidate the products prefix
        await _cache.InvalidatePrefixAsync("products", ct);

        return NoContent();
    }
}
