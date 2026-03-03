using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Application;

public interface ISearchService<TEntity> where TEntity : class
{
    /// <summary>
    /// Direct retrieval by ID (The 'R' in CRUD)
    /// </summary>
    Task<TEntity?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Index or Update a document
    /// </summary>
    Task<ElasticOperationResult> SaveAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Delete a document by ID
    /// </summary>
    Task<ElasticOperationResult> DeleteAsync(string id, CancellationToken ct = default);

    Task<SearchResult<TEntity>> QueryAsync(SearchQuery request, CancellationToken ct = default);
}