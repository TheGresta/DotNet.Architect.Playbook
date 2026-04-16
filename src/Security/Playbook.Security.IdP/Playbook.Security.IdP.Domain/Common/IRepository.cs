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
    // ─── READ — MUTABLE (intended for write operations) ─────────────────────────

    /// <summary>
    /// Asynchronously retrieves a single entity that satisfies the given predicate,
    /// returning a mutable instance suitable for subsequent domain operations and persistence.
    /// Returns <see langword="null"/> when no match is found.
    /// </summary>
    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        IQueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of entities that satisfy the given predicate,
    /// returning mutable instances suitable for subsequent domain operations.
    /// </summary>
    Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        IQueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    // ─── READ — READ-ONLY (optimised for queries; do not mutate results) ────────

    /// <summary>
    /// Asynchronously retrieves a single entity optimised for read-only scenarios.
    /// The returned instance must not be modified or persisted.
    /// Returns <see langword="null"/> when no match is found.
    /// </summary>
    Task<TEntity?> FindOneReadOnlyAsync(
        Expression<Func<TEntity, bool>> predicate,
        IQueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of entities optimised for read-only scenarios.
    /// The returned instances must not be modified or persisted.
    /// </summary>
    Task<List<TEntity>> FindAllReadOnlyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        IQueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    // ─── PROJECTIONS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously projects each entity in a filtered result set into <typeparamref name="TResult"/>.
    /// Use for lightweight reads where only a subset of data is required.
    /// </summary>
    Task<List<TResult>> FindAllProjectedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        IQueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    // ─── EXISTENCE ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously determines whether at least one entity satisfies the predicate.
    /// If <paramref name="predicate"/> is <see langword="null"/>, checks whether the store contains any records.
    /// </summary>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // ─── COMMANDS ────────────────────────────────────────────────────────────────

    /// <summary>Schedules the entity for creation in the underlying store.</summary>
    void Add(TEntity entity);

    /// <summary>Schedules the entity for an update in the underlying store.</summary>
    void Update(TEntity entity);

    /// <summary>
    /// Schedules the entity for logical removal (soft-delete).
    /// Implementations should set <see cref="Entity{TId}.IsActive"/> to <see langword="false"/>
    /// rather than physically removing the record.
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>
    /// Schedules the entity for physical removal from the underlying store.
    /// Use with caution — this operation is irreversible.
    /// </summary>
    void HardDelete(TEntity entity);
}

