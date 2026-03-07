using Microsoft.AspNetCore.Mvc;

using Playbook.Persistence.Redis.Interfaces;
using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis.Controllers;

/// <summary>
/// Provides high-performance API endpoints for product management, utilizing a multi-level cache-aside strategy.
/// </summary>
/// <remarks>
/// This controller coordinates between <see cref="IProductRepository"/> for persistence and <see cref="ICacheService"/> 
/// for low-latency data retrieval. It employs version-based invalidation to ensure data consistency 
/// across distributed nodes without expensive Redis scan operations.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(
    ICacheService cache,
    IProductRepository repository) : ControllerBase
{
    private const string CachePrefix = "products";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Retrieves a specific product by its identifier, checking the cache before querying the repository.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// An <see cref="ActionResult{TValue}"/> containing the <see cref="ProductDto"/> if found; 
    /// otherwise, a <see cref="NotFoundResult"/>.
    /// </returns>
    /// <remarks>
    /// This method uses the <see cref="ICacheService.GetOrSetAsync{T}"/> pattern to prevent cache stampedes 
    /// and automatically populates the cache on a miss.
    /// </remarks>
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

    /// <summary>
    /// Retrieves all products, utilizing the cache to reduce repository load.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A collection of <see cref="ProductDto"/> instances.</returns>
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

    /// <summary>
    /// Creates a new product and invalidates the existing product cache prefix.
    /// </summary>
    /// <param name="product">The product data to create.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The newly created <see cref="ProductDto"/> and the location of the resource.</returns>
    /// <remarks>
    /// Calling <see cref="ICacheService.InvalidatePrefixAsync"/> performs an <c>O(1)</c> version increment 
    /// which logically invalidates all cached items under the "products" prefix simultaneously.
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductDto product, CancellationToken ct)
    {
        var created = await repository.CreateAsync(product, ct);

        await cache.InvalidatePrefixAsync(CachePrefix, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing product and triggers a global cache invalidation for the product prefix.
    /// </summary>
    /// <param name="id">The identifier of the product to update.</param>
    /// <param name="product">The updated product data.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="NoContentResult"/> on success; otherwise, a <see cref="NotFoundResult"/> or <see cref="BadRequestResult"/>.</returns>
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

    /// <summary>
    /// Deletes a product and triggers a global cache invalidation for the product prefix.
    /// </summary>
    /// <param name="id">The identifier of the product to remove.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="NoContentResult"/> if deleted; otherwise, <see cref="NotFoundResult"/>.</returns>
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
