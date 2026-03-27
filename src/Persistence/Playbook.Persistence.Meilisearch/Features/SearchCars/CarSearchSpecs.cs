using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Features.SearchCars;

/// <summary>
/// Encapsulates reusable, domain-specific search specifications for the automotive index.
/// This static utility ensures that business rules—such as default sorting, mandatory filters, 
/// and UI highlighting—are applied consistently across different search entry points.
/// </summary>
/// <remarks>
/// By centralizing these rules in a "Specifications" class, the system remains maintainable 
/// and prevents logic duplication within Handlers or Controllers.
/// </remarks>
public static class CarSearchSpecs
{
    /// <summary>
    /// Applies a standard set of search configurations to a <see cref="MeiliSearchDescriptor{CarDocument}"/>
    /// based on the provided <see cref="SearchCarsRequest"/>.
    /// </summary>
    /// <param name="q">The fluent descriptor used to build the Meilisearch query.</param>
    /// <param name="req">The inbound request containing user-defined parameters.</param>
    public static void ApplyStandardSearch(MeiliSearchDescriptor<CarDocument> q, SearchCarsRequest req) => q
        // Configures result set boundaries for pagination.
        .Paging(req.Limit, req.Offset)

        // Defines the logical filtering tree.
        .WithFilters(f => f
            // Applies an exact match filter for the manufacturer if provided.
            .WhereEquals(x => x.Company, req.Company)

            // Filters for vehicles above a specific price threshold.
            .WhereGreaterThanOrEqual(x => x.PriceUsd, req.MinPrice))

        // Ensures premium vehicles (highest price) are surfaced first by default.
        .SortByDescending(x => x.PriceUsd)

        // Instructs Meilisearch to wrap matching search terms in the 'Model' field 
        // with HTML tags for enhanced UI visibility.
        .Highlight(x => x.Model);
}
