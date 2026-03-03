using System.Diagnostics;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Products.Elasticsearch;
using Playbook.Persistence.ElasticSearch.Application;
using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Persistence;

public sealed class ElasticsearchService<TEntity>(
    ElasticsearchClient client,
    ILogger<ElasticsearchService<TEntity>> logger)
    : ISearchService<TEntity> where TEntity : class
{
    // Convention: Use a static readonly to avoid repeating calculation for every instance
    private static readonly string DefaultIndex = typeof(TEntity).Name.ToLowerInvariant();

    public async Task<ElasticOperationResult> SaveAsync(TEntity entity, CancellationToken ct = default)
    {
        var response = await client.IndexAsync(entity, i => i.Index(DefaultIndex), ct).ConfigureAwait(false);

        return response.IsValidResponse
            ? ElasticOperationResult.Success()
            : LogAndReturnFailure(response, "index");
    }

    public async Task<TEntity?> GetAsync(string id, CancellationToken ct = default)
    {
        // Use Source directly to reduce overhead if the ID is missing
        var response = await client.GetAsync<TEntity>(id, g => g.Index(DefaultIndex), ct).ConfigureAwait(false);
        return response.Source;
    }

    public async Task<ElasticOperationResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        var response = await client.DeleteAsync<TEntity>(id, d => d.Index(DefaultIndex), ct).ConfigureAwait(false);

        return response.IsValidResponse
            ? ElasticOperationResult.Success()
            : LogAndReturnFailure(response, "delete", id);
    }

    public async Task<SearchResult<TEntity>> QueryAsync(SearchQuery request, CancellationToken ct = default)
    {
        var timer = Stopwatch.StartNew();

        var response = await client.SearchAsync<TEntity>(s => s
            .Index(DefaultIndex)
            .From((request.Page - 1) * request.PageSize)
            .Size(request.PageSize)
            .Query(q => q
                .Bool(b => b
                    .Must(m => BuildTermQuery(m, request.Term))
                    .Filter(f => BuildFilters(f, request.Filters))
                )
            )
            .Sort(sort => BuildSort(sort, request)),
            ct).ConfigureAwait(false);

        timer.Stop();

        if (!response.IsValidResponse)
        {
            logger.LogError("Search failed for {Index}: {Debug}", DefaultIndex, response.DebugInformation);
            return new SearchResult<TEntity>(Array.Empty<TEntity>(), 0, timer.Elapsed);
        }

        return new SearchResult<TEntity>(response.Documents, response.Total, timer.Elapsed);
    }

    private static void BuildTermQuery(QueryDescriptor<TEntity> q, string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            q.MatchAll(new MatchAllQuery());
            return;
        }

        // Optimization: Use MultiMatch but avoid "_all". 
        // In a real system, you'd define "SearchableFields" on the entity.
        q.MultiMatch(mm => mm
            .Query(term)
            .Type(TextQueryType.BestFields)
            .Fuzziness(new Fuzziness("AUTO"))
        );
    }

    private static void BuildFilters(QueryDescriptor<TEntity> q, Dictionary<string, object>? filters)
    {
        if (filters is not { Count: > 0 }) return;

        var queries = new List<Query>(filters.Count); // Pre-size the list

        foreach (var (key, value) in filters)
        {
            if (string.IsNullOrWhiteSpace(key) || value is null) continue;

            if (value is IEnumerable<string> values)
            {
                var validValues = values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => FieldValue.String(v))
                    .ToArray();

                if (validValues.Length > 0)
                    queries.Add(new TermsQuery { Field = key, Term = new TermsQueryField(validValues) });
            }
            else
            {
                var val = value.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                    queries.Add(new TermQuery(key) { Value = val });
            }
        }

        if (queries.Count > 0) q.Bool(b => b.Filter(queries.ToArray()));
    }

    private static void BuildSort(SortOptionsDescriptor<TEntity> sort, SearchQuery request)
    {
        if (string.IsNullOrWhiteSpace(request.SortBy)) return;

        sort.Field(request.SortBy, f => f
            .Order(request.SortDescending ? SortOrder.Desc : SortOrder.Asc));
    }

    private ElasticOperationResult LogAndReturnFailure(ElasticsearchResponse response, string op, string? id = null)
    {
        var error = response.ElasticsearchServerError?.Error.Reason ?? "Unknown error";
        logger.LogError("Failed to {Operation} document {Id} in {Index}. Error: {Error}", op, id ?? "", DefaultIndex, error);
        return ElasticOperationResult.Failure(error);
    }
}