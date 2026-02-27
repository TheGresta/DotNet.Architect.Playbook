using System.Linq.Expressions;
using Playbook.Persistence.MongoDB.Domain;

namespace Playbook.Persistence.MongoDB.Application;

/// <summary>
/// Defines the core data access operations for MongoDB documents of type <typeparamref name="TDocument"/>.
/// </summary>
/// <typeparam name="TDocument">The type of the document, which must inherit from <see cref="BaseDocument"/>.</typeparam>
public interface IBaseRepository<TDocument> where TDocument : BaseDocument
{
    #region Querying

    /// <summary>
    /// Asynchronously finds a single document that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, containing the matched document or <see langword="null"/> if no match is found.</returns>
    Task<TDocument?> FindOneAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of documents based on filtering, sorting, and limit criteria.
    /// </summary>
    /// <param name="predicate">An optional function to filter the documents.</param>
    /// <param name="orderBy">An optional function to order the resulting queryable.</param>
    /// <param name="takeTop">An optional integer to limit the number of returned documents.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="List{T}"/> of matched documents.</returns>
    Task<List<TDocument>> FindAllAsync(
        Expression<Func<TDocument, bool>>? predicate = null,
        Func<IQueryable<TDocument>, IOrderedQueryable<TDocument>>? orderBy = null,
        int? takeTop = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a paginated subset of documents.
    /// </summary>
    /// <param name="predicate">An optional function to filter the documents.</param>
    /// <param name="orderBy">An optional function to order the documents before pagination.</param>
    /// <param name="index">The zero-based page index. Defaults to 0.</param>
    /// <param name="size">The number of items per page. Defaults to 10.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="Paginate{T}"/> object with the requested page of data.</returns>
    Task<Paginate<TDocument>> FindAllByPaginateAsync(
        Expression<Func<TDocument, bool>>? predicate = null,
        Func<IQueryable<TDocument>, IOrderedQueryable<TDocument>>? orderBy = null,
        int index = 0,
        int size = 10,
        CancellationToken cancellationToken = default);

    #endregion

    #region Utilities

    /// <summary>
    /// Asynchronously determines whether any document satisfies a condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing <see langword="true"/> if any documents match the predicate; otherwise, <see langword="false"/>.</returns>
    Task<bool> AnyAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the number of documents that satisfy an optional condition.
    /// </summary>
    /// <param name="predicate">An optional function to filter the documents to be counted.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the total count of documents.</returns>
    Task<long> CountAsync(Expression<Func<TDocument, bool>>? predicate = null, CancellationToken cancellationToken = default);

    #endregion

    #region Commands

    /// <summary>
    /// Asynchronously inserts a new document into the collection.
    /// </summary>
    /// <param name="document">The document instance to add.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
    Task AddAsync(TDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously inserts multiple documents into the collection in a bulk operation.
    /// </summary>
    /// <param name="documents">The collection of documents to add.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates an existing document in the collection.
    /// </summary>
    /// <param name="document">The document instance containing updated values.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// The update is typically performed by matching the <see cref="BaseDocument.Id"/>. 
    /// Implementations should handle the <see cref="BaseDocument.Version"/> for optimistic concurrency.
    /// </remarks>
    Task UpdateAsync(TDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes a document from the collection by its unique identifier.
    /// </summary>
    /// <param name="id">The <see cref="Guid"/> of the document to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes a specific document instance from the collection.
    /// </summary>
    /// <param name="document">The document instance to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(TDocument document, CancellationToken cancellationToken = default);

    #endregion
}
