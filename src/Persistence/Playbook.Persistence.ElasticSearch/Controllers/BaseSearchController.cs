using Microsoft.AspNetCore.Mvc;
using Playbook.Persistence.ElasticSearch.Application;
using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(
    ISearchService<Product> searchService,
    ILogger<ProductsController> logger) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var product = await searchService.GetAsync(id, ct);

        return product is not null
            ? Ok(product)
            : NotFound(new { Message = $"Product {id} not found in search index." });
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] Product product, CancellationToken ct)
    {
        var result = await searchService.SaveAsync(product, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await searchService.DeleteAsync(id, ct);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Example: api/products/search?term=laptop&category=electronics&page=1
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<SearchResult<Product>>> Search(
        [FromQuery] string? term,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool desc = true,
        CancellationToken ct = default)
    {
        // Map query params to our domain-agnostic SearchQuery object
        var filters = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(category))
        {
            filters.Add(nameof(Product.Category).ToLowerInvariant(), category);
        }

        var query = new SearchQuery(
            Term: term,
            Page: page,
            PageSize: pageSize,
            Filters: filters,
            SortBy: sortBy,
            SortDescending: desc
        );

        var results = await searchService.QueryAsync(query, ct);
        return Ok(results);
    }
}
public record Product
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Category { get; init; }
    public int Stock { get; init; }
}