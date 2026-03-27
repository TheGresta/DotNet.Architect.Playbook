using Meilisearch;

using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Features.SearchCars;

public sealed class SearchCarsHandler(MeiliContext context)
{
    public async Task<SearchResult<CarDocument>> HandleAsync(SearchCarsRequest request, CancellationToken ct = default)
    {
        var index = context.GetIndex();

        // THE GOLD STANDARD: Complete Type-Safe Orchestration
        var query = new MeiliSearchDescriptor<CarDocument>(request.SearchTerm)
            .Paging(request.Limit, request.Offset)
            .WithFilters(f => f
                .WhereEquals(x => x.Company, request.Company?.ToUpperInvariant())
                .WhereEquals(x => x.FuelType, request.FuelType)
                .WhereGreaterThanOrEqual(x => x.PriceUsd, request.MinPrice)
                .WhereLessThanOrEqual(x => x.PriceUsd, request.MaxPrice))
            .SortByDescending(x => x.PriceUsd) // Sort by most expensive first
            .SortBy(x => x.Horsepower)         // Then by horsepower ascending
            .Select(x => x.Id, x => x.Company, x => x.Model, x => x.PriceUsd, x => x.FuelType)
            .Facets(x => x.Company, x => x.FuelType)
            .Highlight(x => x.Model, x => x.Company)
            .Build();

        var result = await index.SearchAsync<CarDocument>(query.Q, query, ct);
        return (SearchResult<CarDocument>)result;
    }
}
