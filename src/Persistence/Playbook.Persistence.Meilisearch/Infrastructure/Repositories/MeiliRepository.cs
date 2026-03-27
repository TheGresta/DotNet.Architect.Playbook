using Meilisearch;

using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

public class MeiliRepository<T>(
    MeiliContext context) : IMeiliRepository<T> where T : class
{
    private readonly MeiliContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<SearchResponse<T>> SearchAsync(
        string? query,
        Action<MeiliSearchDescriptor<T>>? configure = null,
        CancellationToken ct = default)
    {
        var descriptor = new MeiliSearchDescriptor<T>(query);
        configure?.Invoke(descriptor);

        var searchQuery = descriptor.Build();
        var index = _context.GetIndex();

        var result = await index.SearchAsync<T>(query, searchQuery, cancellationToken: ct).ConfigureAwait(false);

        return MapToResponse(result);
    }

    public async Task AddOrUpdateAsync(IEnumerable<T> documents, CancellationToken ct = default)
    {
        var index = _context.GetIndex();
        var task = await index.AddDocumentsAsync(documents, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var index = _context.GetIndex();
        await index.DeleteDocumentsAsync(ids, ct).ConfigureAwait(false);
    }

    public async Task TruncateAsync(CancellationToken ct = default)
    {
        var index = _context.GetIndex();
        await index.DeleteAllDocumentsAsync(ct).ConfigureAwait(false);
    }

    private static SearchResponse<T> MapToResponse(ISearchable<T> result)
    {
        long totalCount = 0;
        int? limit = null;
        int? offset = null;

        // Pattern matching to check which type of result Meilisearch returned
        if (result is SearchResult<T> finiteResult)
        {
            // These properties ONLY exist on SearchResult<T>
            totalCount = finiteResult.EstimatedTotalHits;
            limit = finiteResult.Limit;
            offset = finiteResult.Offset;
        }

        return new SearchResponse<T>(
            Items: [.. result.Hits],
            TotalCount: totalCount,
            Limit: limit,
            Offset: offset,
            Facets: result.FacetDistribution
        );
    }
}
