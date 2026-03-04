using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Application.Documents;

public class Product : BaseDocument
{
    [SearchableField(Boost = 3)]
    public required string Name { get; init; }
    [SearchableField]
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Category { get; init; }
    public int Stock { get; init; }
}