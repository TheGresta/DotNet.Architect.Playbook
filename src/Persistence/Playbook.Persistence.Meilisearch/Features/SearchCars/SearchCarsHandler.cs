using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

namespace Playbook.Persistence.Meilisearch.Features.SearchCars;

/// <summary>
/// Provides a clean, decoupled entry point for executing automotive search operations.
/// This handler follows the Mediator pattern principle, encapsulating the orchestration 
/// logic between the inbound request DTO and the underlying search infrastructure.
/// </summary>
/// <remarks>
/// By isolating the search logic into a dedicated handler, the system ensures that 
/// controllers or minimal API endpoints remain thin, while complex query 
/// specifications are delegated to the <see cref="CarSearchSpecs"/> utility.
/// </remarks>
public sealed class SearchCarsHandler(ICarDocumentRepository repository)
{
    /// <summary>
    /// Processes an asynchronous search request by translating the <see cref="SearchCarsRequest"/> 
    /// into a type-safe Meilisearch query and executing it via the repository.
    /// </summary>
    /// <param name="request">The search parameters including terms, filters, and pagination.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the search task to complete.</param>
    /// <returns>A <see cref="SearchResponse{CarDocument}"/> containing the filtered hits and search metadata.</returns>
    public async Task<SearchResponse<CarDocument>> HandleAsync(SearchCarsRequest request, CancellationToken ct = default)
        // Orchestrates the repository search by applying the domain-specific search specifications.
        => await repository.SearchAsync(
            request.SearchTerm,
            q => CarSearchSpecs.ApplyStandardSearch(q, request),
            ct);
}
