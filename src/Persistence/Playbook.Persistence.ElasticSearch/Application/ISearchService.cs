using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Application;

/// <summary>
/// Defines the contract for a service that manages and queries documents of type <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The document type stored in the search index.</typeparam>
public interface ISearchService<TEntity> where TEntity : BaseDocument
{
    /// <summary>
    /// Retrieves a single document by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, containing the document if found; otherwise, <see langword="null"/>.</returns>
    ValueTask<TEntity?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Persists or updates a single document in the search index.
    /// </summary>
    /// <param name="entity">The document instance to save.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the <see cref="SearchOperationResult"/> of the save operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    Task<SearchOperationResult> SaveAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Persists or updates a collection of documents in a single bulk operation.
    /// </summary>
    /// <param name="entities">The collection of documents to save.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the <see cref="SearchOperationResult"/> summarizing the bulk operation.</returns>
    Task<SearchOperationResult> BulkSaveAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    /// <summary>
    /// Removes a document from the search index by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document to delete.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the <see cref="SearchOperationResult"/> of the deletion.</returns>
    Task<SearchOperationResult> DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Executes a complex search query against the index with pagination support.
    /// </summary>
    /// <param name="request">The <see cref="SearchQuery{T}"/> containing filters, sorting, and pagination parameters.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="SearchPageResponse{T}"/> with the filtered results.</returns>
    Task<SearchPageResponse<TEntity>> QueryAsync(SearchQuery<TEntity> request, CancellationToken ct = default);
}