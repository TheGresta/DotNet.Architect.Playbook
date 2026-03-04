using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Application;

public interface ISearchService<TEntity> where TEntity : BaseDocument
{
    ValueTask<TEntity?> GetAsync(string id, CancellationToken ct = default);

    Task<SearchOperationResult> SaveAsync(TEntity entity, CancellationToken ct = default);

    Task<SearchOperationResult> BulkSaveAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    Task<SearchOperationResult> DeleteAsync(string id, CancellationToken ct = default);

    Task<SearchPageResponse<TEntity>> QueryAsync(SearchQuery<TEntity> request, CancellationToken ct = default);
}