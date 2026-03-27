namespace Playbook.Persistence.Meilisearch.Core.Models;

/// <summary>
/// Represents a high-level, immutable search result envelope.
/// This record standardizes the delivery of search hits and engine metadata across the application,
/// ensuring a consistent contract for UI components and API consumers.
/// </summary>
/// <typeparam name="T">The type of the document model returned in the search results.</typeparam>
/// <param name="Items">The collection of documents matching the search criteria.</param>
/// <param name="TotalCount">
/// The total number of documents matching the query (estimated or exact, depending on the engine configuration).
/// </param>
/// <param name="Limit">The maximum number of items requested in the current page.</param>
/// <param name="Offset">The number of items skipped from the beginning of the result set.</param>
/// <param name="Facets">
/// A multi-dimensional dictionary containing distribution data for filterable attributes.
/// Structure: [AttributeName][Value] = Count.
/// </param>
public record SearchResponse<T>(
    IReadOnlyList<T> Items,
    long TotalCount,
    int? Limit,
    int? Offset,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>? Facets
);
