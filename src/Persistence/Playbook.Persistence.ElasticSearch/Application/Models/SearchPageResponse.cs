namespace Playbook.Persistence.ElasticSearch.Application.Models;

/// <summary>
/// Represents a paginated response containing search results and related metadata.
/// </summary>
/// <typeparam name="T">The type of document, which must inherit from <see cref="BaseDocument"/>.</typeparam>
/// <param name="Items">The collection of documents found for the current page.</param>
/// <param name="TotalCount">The total number of documents matching the query across all pages.</param>
/// <param name="CurrentPage">The index of the current page returned.</param>
/// <param name="PageSize">The number of items requested per page.</param>
/// <param name="ExecutionTime">The duration taken by the search engine to process the request.</param>
public record SearchPageResponse<T>(
    IReadOnlyCollection<T> Items,
    long TotalCount,
    int CurrentPage,
    int PageSize,
    TimeSpan ExecutionTime) where T : BaseDocument
{
    /// <summary>
    /// Gets the total number of pages available based on the <see cref="TotalCount"/> and <see cref="PageSize"/>.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a subsequent page of results available.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;
}
