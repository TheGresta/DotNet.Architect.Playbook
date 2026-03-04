using System.Diagnostics;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Products.Elasticsearch;
using Playbook.Persistence.ElasticSearch.Application;
using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Persistence;

public sealed class ElasticsearchService<T>(
    ElasticsearchClient client,
    ILogger<ElasticsearchService<T>> logger)
    : ISearchService<T> where T : BaseDocument
{
    private static readonly string IndexName = typeof(T).Name.ToLowerInvariant();

    public async ValueTask<T?> GetAsync(string id, CancellationToken ct = default)
    {
        var response = await client.GetAsync<T>(id, ct);

        if (response.IsSuccess())
        {
            return response.Source;
        }

        logger.LogWarning("Elasticsearch: Get failed for {Id}. Debug: {Reason}", id, response.DebugInformation);
        return null;
    }

    public async Task<SearchOperationResult> SaveAsync(T entity, CancellationToken ct = default)
    {
        var response = await client.IndexAsync(entity, ct);
        return HandleResponse(response, "Indexing failed");
    }

    public async Task<SearchOperationResult> BulkSaveAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        if (!entities.Any()) return SearchOperationResult.Success();

        var bulkResponse = await client.BulkAsync(b => b
            .Index(IndexName)
            .IndexMany(entities)
            .Refresh(Refresh.WaitFor), ct);

        if (bulkResponse.IsSuccess())
        {
            return SearchOperationResult.Success();
        }

        var errorCount = bulkResponse.ItemsWithErrors.Count();
        logger.LogError("Elasticsearch: Bulk indexing had {Count} errors. Debug: {Debug}", errorCount, bulkResponse.DebugInformation);

        return SearchOperationResult.Failure($"Bulk operation failed with {errorCount} errors.");
    }

    public async Task<SearchOperationResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        var response = await client.DeleteAsync<T>(id, ct);
        return HandleResponse(response, $"Delete failed for {id}");
    }

    public async Task<SearchPageResponse<T>> QueryAsync(SearchQuery<T> request, CancellationToken ct = default)
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