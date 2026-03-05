using Microsoft.AspNetCore.Mvc;
using Playbook.Persistence.Redis.Interfaces;
using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(
    ICacheService cache,
    IProductRepository repository) : ControllerBase
{
    private const string CachePrefix = "products";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        try
        {
            var product = await cache.GetOrSetAsync(
                CachePrefix,
                id.ToString(),
                async token => await repository.GetByIdAsync(id, token)
                               ?? throw new HttpRequestException("Not Found", null, System.Net.HttpStatusCode.NotFound),
                DefaultTtl,
                ct);

            return Ok(product);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound($"Product with ID {id} not found.");
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(CancellationToken ct)
    {
        var products = await cache.GetOrSetAsync(
            CachePrefix,
            "all",
            token => repository.GetAllAsync(token),
            DefaultTtl,
            ct);

        return Ok(products);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductDto product, CancellationToken ct)
    {
        var created = await repository.CreateAsync(product, ct);

        // O(1) Versioned Invalidation: Instantly makes "all" and individual product caches stale
        await cache.InvalidatePrefixAsync(CachePrefix, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductDto product, CancellationToken ct)
    {
        if (id != product.Id)
        {
            return BadRequest("ID mismatch between route and body.");
        }

        var updated = await repository.UpdateAsync(product, ct);
        if (updated is null)
        {
            return NotFound();
        }

        await cache.InvalidatePrefixAsync(CachePrefix, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound();
        }

        await cache.InvalidatePrefixAsync(CachePrefix, ct);

        return NoContent();
    }
}