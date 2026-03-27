using Meilisearch;

using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

/// <summary>
/// A high-performance, generic implementation of the <see cref="IMeiliRepository{T}"/> interface.
/// This repository acts as a bridge between the domain model and the Meilisearch engine, 
/// utilizing the <see cref="MeiliContext"/> for index resolution and <see cref="MeiliSearchDescriptor{T}"/> 
/// for type-safe query construction.
/// </summary>
/// <typeparam name="T">The document type stored in the Meilisearch index.</typeparam>
public class MeiliRepository<T>(
    MeiliContext context) : IMeiliRepository<T> where T : class
{
    private readonly MeiliContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// Executes an asynchronous search against the Meilisearch index using a fluent descriptor 
    /// to build complex filters, sorting, and pagination.
    /// </summary>
    /// <param name="query">The search term provided by the user.</param>
    /// <param name="configure">An optional delegate to define search constraints via <see cref="MeiliSearchDescriptor{T}"/>.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A specialized <see cref="SearchResponse{T}"/> containing hits and search metadata.</returns>
    public async Task<SearchResponse<T>> SearchAsync(
        string? query,
        Action<MeiliSearchDescriptor<T>>? configure = null,
        CancellationToken ct = default)
    {
        // Initialize the descriptor with the base search term.
        var descriptor = new MeiliSearchDescriptor<T>(query);

        // Apply user-defined configurations (filters, facets, sorts).
        configure?.Invoke(descriptor);

        var searchQuery = descriptor.Build();
        var index = _context.GetIndex();

        // ✅ Pass null as the first arg — the search term is already in searchQuery.Q
        var result = await index.SearchAsync<T>(null, searchQuery, cancellationToken: ct).ConfigureAwait(false);

        return MapToResponse(result);
    }

    /// <summary>
    /// Batch upserts documents into the index. Meilisearch handles this asynchronously via a task queue.
    /// </summary>
    public async Task AddOrUpdateAsync(IEnumerable<T> documents, CancellationToken ct = default)
    {
        var index = _context.GetIndex();

        // AddDocumentsAsync pushes the payload to the Meilisearch task queue.
        await index.AddDocumentsAsync(documents, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a specific set of documents from the index by their primary keys.
    /// </summary>
    public async Task DeleteAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var index = _context.GetIndex();
        await index.DeleteDocumentsAsync(ids, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Purges all documents from the index. Useful for full re-indexing operations.
    /// </summary>
    public async Task TruncateAsync(CancellationToken ct = default)
    {
        var index = _context.GetIndex();
        await index.DeleteAllDocumentsAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps the polymorphic <see cref="ISearchable{T}"/> result from the Meilisearch SDK 
    /// into a unified <see cref="SearchResponse{T}"/> record.
    /// </summary>
    /// <param name="result">The raw search result from the client.</param>
    /// <returns>A flattened, consumer-friendly search response.</returns>
    private static SearchResponse<T> MapToResponse(ISearchable<T> result)
    {
        long totalCount = 0;
        int? limit = null;
        int? offset = null;

        // Meilisearch returns different concrete types depending on the query parameters (e.g., hits vs. finite hits).
        // We use C# pattern matching to safely extract metadata if it exists.
        if (result is SearchResult<T> finiteResult)
        {
            totalCount = finiteResult.EstimatedTotalHits;
            limit = finiteResult.Limit;
            offset = finiteResult.Offset;
        }
        else
        {
            // Log or handle unexpected result
        }

        return new SearchResponse<T>(
            Items: [.. result.Hits], // Uses the spread operator for efficient collection expression conversion.
            TotalCount: totalCount,
            Limit: limit,
            Offset: offset,
            Facets: result.FacetDistribution
        );
    }
}
