using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Playbook.Persistence.ElasticSearch.Application;
using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ISearchService<Product> searchService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var product = await searchService.GetAsync(id, ct);
        return product is not null ? Ok(product) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product, CancellationToken ct)
    {
        var result = await searchService.SaveAsync(product, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = product.Id }, product)
            : BadRequest(result.ErrorMessage);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] IEnumerable<Product> products, CancellationToken ct)
    {
        var result = await searchService.BulkSaveAsync(products, ct);
        return result.IsSuccess ? Ok("Bulk indexing completed.") : BadRequest(result.ErrorMessage);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await searchService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }

    [HttpGet("search")]
    public async Task<ActionResult<SearchPageResponse<Product>>> Search(
        [FromQuery] string? term,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool sortDescending = false,
        CancellationToken ct = default)
    {
        // Guard against invalid pagination
        var validatedPage = Math.Max(1, page);
        var validatedSize = Math.Clamp(pageSize, 1, 100);

        var query = BuildSearchQuery(term, category, validatedPage, validatedSize, sortBy, sortDescending);

        var response = await searchService.QueryAsync(query, ct);

        return Ok(response);
    }

    #region Private Helpers

    private static SearchQuery<Product> BuildSearchQuery(
        string? term, string? category, int page, int pageSize, string? sortBy, bool sortDescending)
    {
        var query = new SearchQuery<Product>
        {
            Term = term,
            Page = page,
            PageSize = pageSize,
            SortDescending = sortDescending,
            SortByExpression = MapSortExpression(sortBy)
        };

        if (!string.IsNullOrWhiteSpace(category))
        {
            query.Filters.Add(p => p.Category!, category);
        }

        return query;
    }

    private static Expression<Func<Product, object>> MapSortExpression(string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "price" => p => p.Price,
            "stock" => p => p.Stock,
            _ => p => p.Name
        };

    #endregion
}
public class Product : BaseDocument
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Category { get; init; }
    public int Stock { get; init; }
}