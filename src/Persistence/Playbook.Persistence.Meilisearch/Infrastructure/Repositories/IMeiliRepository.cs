using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

/// <summary>
/// Defines a high-performance, generic architectural contract for Meilisearch persistence operations.
/// This interface abstracts the underlying search engine complexities, providing a unified 
/// pattern for document indexing, lifecycle management, and type-safe querying.
/// </summary>
/// <typeparam name="T">The document model type, representing the schema of the search index.</typeparam>
public interface IMeiliRepository<T> where T : class
{
    /// <summary>
    /// Executes a type-safe search query against the Meilisearch index.
    /// </summary>
    /// <param name="query">The raw search terms or string to match against documents.</param>
    /// <param name="configure">An optional delegate to configure advanced search parameters (filters, sorting, facets) using a fluent descriptor.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the search task to complete.</param>
    /// <returns>A <see cref="SearchResponse{T}"/> containing the matched hits, facets, and metadata.</returns>
    Task<SearchResponse<T>> SearchAsync(string? query, Action<MeiliSearchDescriptor<T>>? configure = null, CancellationToken ct = default);

    /// <summary>
    /// Upserts a collection of documents into the index. 
    /// If a document with the same primary key exists, it is updated; otherwise, it is created.
    /// </summary>
    /// <param name="documents">The collection of entities to be indexed.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous indexing operation.</returns>
    Task AddOrUpdateAsync(IEnumerable<T> documents, CancellationToken ct = default);

    /// <summary>
    /// Removes a batch of documents from the index based on their unique identifiers.
    /// </summary>
    /// <param name="ids">A collection of unique document IDs to be deleted.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous deletion operation.</returns>
    Task DeleteAsync(IEnumerable<string> ids, CancellationToken ct = default);

    /// <summary>
    /// Completely clears all documents from the index while preserved the index settings and schema.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous truncation operation.</returns>
    Task TruncateAsync(CancellationToken ct = default);
}
