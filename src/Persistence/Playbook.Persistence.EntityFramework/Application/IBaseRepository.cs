using System.Linq.Expressions;
using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Application;

public interface IBaseRepository<TEntity> where TEntity : Entity
{
    // --- READ METHODS (TRACKED) ---
    // Used for "Fetch then Update" scenarios
    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? takeTop = null,
        CancellationToken cancellationToken = default);

    // --- READ METHODS (NO-TRACKING) ---
    // High-performance, read-only methods
    Task<TEntity?> FindOneAsNoTrackingAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> FindAllAsNoTrackingAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? takeTop = null,
        CancellationToken cancellationToken = default);

    // --- PROJECTIONS (Always No-Tracking by nature) ---
    Task<List<TResult>> FindAllSelectedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? takeTop = null,
        CancellationToken cancellationToken = default);

    // --- PAGINATION ---
    Task<Paginate<TEntity>> FindAllByPaginateAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int index = 0,
        int size = 10,
        CancellationToken cancellationToken = default);

    // --- COMMANDS ---
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    void HardDelete(TEntity entity);
}
