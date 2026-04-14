using System.Linq.Expressions;

namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Defines the generic data access contract for entities of type <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The type of the entity, which must derive from <see cref="Entity"/>.</typeparam>
public interface IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    #region READ METHODS (TRACKED)

    /// <summary>
    /// Asynchronously finds a single entity that matches the specified predicate, with change tracking enabled.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order the elements.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> where the result is the found <typeparamref name="TEntity"/>, 
    /// or <see langword="null"/> if no match is found.
    /// </returns>
    /// <remarks>
    /// Use this method when you intend to modify the returned entity and persist changes via a Unit of Work.
    /// </remarks>
    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of tracked entities based on the provided filter and ordering.
    /// </summary>
    /// <param name="predicate">An optional filter to apply to the query.</param>
    /// <param name="orderBy">An optional ordering function.</param>
    /// <param name="skip">The number of elements to skip from the start of the results.</param>
    /// <param name="takeTop">The maximum number of elements to return.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="List{T}"/> of matching entities.</returns>
    Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? takeTop = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region READ METHODS (NO-TRACKING)

    /// <summary>
    /// Asynchronously finds a single entity for read-only purposes, bypassing the change tracker.
    /// </summary>
    /// <inheritdoc cref="FindOneAsync(Expression{Func{TEntity, bool}}, Func{IQueryable{TEntity}, IOrderedQueryable{TEntity}}?, CancellationToken)"/>
    Task<TEntity?> FindOneAsNoTrackingAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of entities without change tracking, optimized for high-performance read operations.
    /// </summary>
    /// <inheritdoc cref="FindAllAsync(Expression{Func{TEntity, bool}}?, Func{IQueryable{TEntity}, IOrderedQueryable{TEntity}}?, int?, int?, CancellationToken)"/>
    Task<List<TEntity>> FindAllAsNoTrackingAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? takeTop = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region PROJECTIONS

    /// <summary>
    /// Asynchronously projects each element of a filtered collection into a new form.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the <paramref name="selector"/>.</typeparam>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="predicate">An optional filter to apply.</param>
    /// <param name="orderBy">An optional ordering function.</param>
    /// <param name="skip">The number of elements to skip.</param>
    /// <param name="takeTop">The maximum number of elements to return.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="List{T}"/> of projected results.</returns>
    Task<List<TResult>> FindAllSelectedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? takeTop = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region EXISTENCE CHECKS

    /// <summary>
    /// Asynchronously determines whether any entity exists that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">An optional filter to apply. If <see langword="null"/>, checks if the table contains any records.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns><see langword="true"/> if at least one entity matches; otherwise, <see langword="false"/>.</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    #endregion

    #region COMMANDS

    /// <summary>
    /// Begins tracking the given entity in the <see cref="F:Microsoft.EntityFrameworkCore.EntityState.Added"/> state.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    void Add(TEntity entity);

    /// <summary>
    /// Begins tracking the given entity in the <see cref="F:Microsoft.EntityFrameworkCore.EntityState.Modified"/> state.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Marks the entity for deletion. Depending on implementation, this may perform a soft-delete (e.g., setting <see cref="Entity.IsActive"/> to false).
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    void Delete(TEntity entity);

    /// <summary>
    /// Marks the entity for physical removal from the database.
    /// </summary>
    /// <param name="entity">The entity to permanently remove.</param>
    void HardDelete(TEntity entity);

    #endregion
}

