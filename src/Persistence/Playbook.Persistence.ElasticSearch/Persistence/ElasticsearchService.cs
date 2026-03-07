using System.Diagnostics;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Products.Elasticsearch;

using Playbook.Persistence.ElasticSearch.Application;
using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Persistence;

/// <summary>
/// Provides a concrete implementation of <see cref="ISearchService{TEntity}"/> using the official Elasticsearch .NET client.
/// </summary>
/// <typeparam name="T">The document type, which must inherit from <see cref="BaseDocument"/>.</typeparam>
/// <remarks>
/// This service automatically determines the index name based on the lowercase name of the type <typeparamref name="T"/>.
/// It utilizes primary constructors for dependency injection of the <see cref="ElasticsearchClient"/> and logging infrastructure.
/// </remarks>
public sealed class ElasticsearchService<T>(
    ElasticsearchClient client,
    ILogger<ElasticsearchService<T>> logger)
    : ISearchService<T> where T : BaseDocument
{
    /// <summary>
    /// The name of the Elasticsearch index, derived from the type name in lowercase.
    /// </summary>
    private static readonly string IndexName = typeof(T).Name.ToLowerInvariant();

    /// <inheritdoc/>
    /// <remarks>
    /// If the document is not found or the request fails, a warning is logged and <see langword="null"/> is returned.
    /// </remarks>
    public async ValueTask<T?> GetAsync(string id, CancellationToken ct)
    {
        var response = await client.GetAsync<T>(id, g => g.Index(IndexName), ct);

        if (response.IsSuccess())
        {
            return response.Source;
        }

        logger.LogWarning("Elasticsearch: Get failed for {Id}. Debug: {Reason}", id, response.DebugInformation);
        return null;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown if the entity is null.</exception>
    public async Task<SearchOperationResult> SaveAsync(T entity, CancellationToken ct)
    {
        var response = await client.IndexAsync(entity, i => i.Index(IndexName), ct);
        return HandleResponse(response, "Indexing failed");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method uses the <c>Refresh.WaitFor</c> policy to ensure that the changes are visible to subsequent 
    /// search requests immediately after the task completes.
    /// </remarks>
    public async Task<SearchOperationResult> BulkSaveAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        if (!entities.Any()) return SearchOperationResult.Success();

        var bulkResponse = await client.BulkAsync(b => b
            .Index(IndexName)
            .IndexMany(entities)
            .Refresh(Refresh.WaitFor), ct);

        // 1. Transport-level failure (network, auth, serialization, etc.)
        if (!bulkResponse.IsSuccess())
        {
            logger.LogError("Elasticsearch: Bulk request failed at transport level. Debug: {Debug}", bulkResponse.DebugInformation);
            return SearchOperationResult.Failure("Bulk operation transport failure.");
        }

        // 2. Per-item failures within an HTTP 200 response
        if (bulkResponse.Errors)
        {
            var errorCount = bulkResponse.ItemsWithErrors.Count();
            logger.LogError("Elasticsearch: Bulk indexing had {Count} item-level errors. Debug: {Debug}", errorCount, bulkResponse.DebugInformation);
            return SearchOperationResult.Failure($"Bulk operation failed with {errorCount} item errors.");
        }

        return SearchOperationResult.Success();
    }

    /// <inheritdoc/>
    public async Task<SearchOperationResult> DeleteAsync(string id, CancellationToken ct)
    {
        var response = await client.DeleteAsync<T>(id, d => d.Index(IndexName), ct);
        return HandleResponse(response, $"Delete failed for {id}");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calculates execution time using <see cref="Stopwatch.GetTimestamp"/> for high-precision measurement.
    /// If the search engine returns an error, an empty <see cref="SearchPageResponse{T}"/> is returned and the error is logged.
    /// </remarks>
    public async Task<SearchPageResponse<T>> QueryAsync(SearchQuery<T> request, CancellationToken ct)
    {
        var timestamp = Stopwatch.GetTimestamp();

        var response = await client.SearchAsync<T>(s => s
            .Index(IndexName)
            .From(request.Skip)
            .Size(request.PageSize)
            .ApplyDynamicQuery(request.Filters, request.Term)
            .ApplySort(request.SortByExpression, request.SortDescending)
        , ct);

        var elapsed = Stopwatch.GetElapsedTime(timestamp);

        if (!response.IsSuccess())
        {
            logger.LogError("Elasticsearch: Query failed. {Error}", response.DebugInformation);
            return new([], 0, request.Page, request.PageSize, elapsed);
        }

        return new(
            Items: [.. response.Documents],
            TotalCount: response.Total,
            CurrentPage: request.Page,
            PageSize: request.PageSize,
            ExecutionTime: elapsed
        );
    }

    #region Helpers

    /// <summary>
    /// Processes an <see cref="ElasticsearchResponse"/> and converts it into a unified <see cref="SearchOperationResult"/>.
    /// </summary>
    /// <param name="response">The raw response from the Elasticsearch client.</param>
    /// <param name="errorPrefix">A contextual prefix for the log message if the operation fails.</param>
    /// <returns>A <see cref="SearchOperationResult"/> indicating success or containing the server's error reason.</returns>
    private SearchOperationResult HandleResponse(ElasticsearchResponse response, string errorPrefix)
    {
        if (response.IsSuccess())
        {
            return SearchOperationResult.Success();
        }

        var reason = response.ElasticsearchServerError?.Error.Reason ?? "Unknown Error";
        logger.LogError("Elasticsearch: {Prefix}. {Error}", errorPrefix, response.DebugInformation);

        return SearchOperationResult.Failure(reason);
    }

    #endregion
}
