using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Playbook.Persistence.EntityFramework.Application;
using Playbook.Persistence.EntityFramework.Domain.Base;
using Playbook.Persistence.EntityFramework.Persistence.Context;
using Playbook.Persistence.EntityFramework.Persistence.Extensions;

namespace Playbook.Persistence.EntityFramework.Persistence;

internal class BaseRepository<TEntity>(ApplicationDbContext context) : IBaseRepository<TEntity>
    where TEntity : Entity
{
    protected readonly ApplicationDbContext _context = context;
    protected readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    #region Read Methods (Tracked)

    public async Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        CancellationToken cancellationToken)
    {
        return await ApplyQuery(predicate, orderBy, enableTracking: true)
            .FirstOrDefaultAsync(cancellationToken);
    }

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

    public async Task<TEntity?> FindOneAsNoTrackingAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        CancellationToken cancellationToken)
    {
        return await ApplyQuery(predicate, orderBy, enableTracking: false)
            .FirstOrDefaultAsync(cancellationToken);
    }

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

    public async Task<List<TResult>> FindAllSelectedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        int? skip,
        int? takeTop,
        CancellationToken cancellationToken)
    {
        // Projections are naturally No-Tracking
        var query = ApplyQuery(predicate, orderBy, enableTracking: false);

        if (skip.HasValue) query = query.Skip(skip.Value);
        if (takeTop.HasValue) query = query.Take(takeTop.Value);

        return await query.Select(selector).ToListAsync(cancellationToken);
    }

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

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate, CancellationToken cancellationToken)
    {
        return predicate == null
            ? await _dbSet.AnyAsync(cancellationToken)
            : await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public void Add(TEntity entity) => _dbSet.Add(entity);

    public void Update(TEntity entity) => _dbSet.Update(entity);

    public void Delete(TEntity entity)
    {
        // Soft delete implementation
        entity.IsActive = false;
        Update(entity);
    }

    public void HardDelete(TEntity entity) => _dbSet.Remove(entity);

    #endregion

    #region Helpers

    /// <summary>
    /// Centralized query builder to handle common logic for all read operations.
    /// </summary>
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
