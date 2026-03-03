using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Playbook.Persistence.EntityFramework.Application;
using Playbook.Persistence.EntityFramework.Domain.Base;
using Playbook.Persistence.EntityFramework.Persistence.Context;
using Playbook.Persistence.EntityFramework.Persistence.Extensions;

namespace Playbook.Persistence.EntityFramework.Persistence;

/// <summary>
/// Provides the standard Entity Framework Core implementation for data access operations.
/// </summary>
/// <typeparam name="TEntity">The type of the domain entity.</typeparam>
internal class BaseRepository<TEntity>(ApplicationDbContext context) : IBaseRepository<TEntity>
    where TEntity : Entity
{
    /// <summary>
    /// The underlying database context for this repository.
    /// </summary>
    protected readonly ApplicationDbContext _context = context;

    /// <summary>
    /// The <see cref="DbSet{TEntity}"/> for the current entity type, used to perform CRUD operations.
    /// </summary>
    protected readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    #region Read Methods (Tracked)

    /// <inheritdoc/>
    public async Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        CancellationToken cancellationToken)
    {
        return await ApplyQuery(predicate, orderBy, enableTracking: true)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        int? skip,
        int? takeTop,
        CancellationToken cancellationToken)
    {
        var query = ApplyQuery(predicate, orderBy, enableTracking: true);

        if (skip.HasValue) query = query.Skip(skip.Value);
        if (takeTop.HasValue) query = query.Take(takeTop.Value);

        return await query.ToListAsync(cancellationToken);
    }

    #endregion

    #region Read Methods (AsNoTracking)

    /// <inheritdoc/>
    public async Task<TEntity?> FindOneAsNoTrackingAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        CancellationToken cancellationToken)
    {
        return await ApplyQuery(predicate, orderBy, enableTracking: false)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<TEntity>> FindAllAsNoTrackingAsync(
        Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        int? skip,
        int? takeTop,
        CancellationToken cancellationToken)
    {
        var query = ApplyQuery(predicate, orderBy, enableTracking: false);

        if (skip.HasValue) query = query.Skip(skip.Value);
        if (takeTop.HasValue) query = query.Take(takeTop.Value);

        return await query.ToListAsync(cancellationToken);
    }

    #endregion

    #region Projections & Pagination

    /// <inheritdoc/>
    public async Task<List<TResult>> FindAllSelectedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        int? skip,
        int? takeTop,
        CancellationToken cancellationToken)
    {
        // Projections ignore the change tracker as the result is typically a DTO or a primitive.
        var query = ApplyQuery(predicate, orderBy, enableTracking: false);

        if (skip.HasValue) query = query.Skip(skip.Value);
        if (takeTop.HasValue) query = query.Take(takeTop.Value);

        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Paginate<TEntity>> FindAllByPaginateAsync(
        Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        int index,
        int size,
        CancellationToken cancellationToken)
    {
        var query = ApplyQuery(predicate, orderBy, enableTracking: false);

        return await query.ToPaginateAsync(index, size, cancellationToken);
    }

    #endregion

    #region Commands

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate, CancellationToken cancellationToken)
    {
        return predicate == null
            ? await _dbSet.AnyAsync(cancellationToken)
            : await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(TEntity entity) => _dbSet.Add(entity);

    /// <inheritdoc/>
    public void Update(TEntity entity) => _dbSet.Update(entity);

    /// <summary>
    /// Performs a logical deletion by setting the <see cref="Entity.IsActive"/> flag to <see langword="false"/>.
    /// </summary>
    /// <param name="entity">The entity to soft-delete.</param>
    /// <remarks>
    /// This relies on the global query filter configured in the persistence layer to exclude the entity from future queries.
    /// </remarks>
    public void Delete(TEntity entity)
    {
        entity.IsActive = false;
        Update(entity);
    }

    /// <inheritdoc/>
    public void HardDelete(TEntity entity) => _dbSet.Remove(entity);

    #endregion

    #region Helpers

    /// <summary>
    /// Constructs an <see cref="IQueryable{T}"/> based on the specified criteria, 
    /// centralizing tracking and filtering logic.
    /// </summary>
    /// <param name="predicate">Optional filter expression.</param>
    /// <param name="orderBy">Optional sorting logic.</param>
    /// <param name="enableTracking">Determines if the EF Core Change Tracker should monitor the results.</param>
    /// <returns>A configured <see cref="IQueryable{TEntity}"/> ready for execution.</returns>
    private IQueryable<TEntity> ApplyQuery(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool enableTracking = true)
    {
        IQueryable<TEntity> query = _dbSet;

        if (!enableTracking) query = query.AsNoTracking();
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null) query = orderBy(query);

        return query;
    }

    #endregion
}