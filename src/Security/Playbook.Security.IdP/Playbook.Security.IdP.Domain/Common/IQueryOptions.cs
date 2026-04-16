namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Defines persistence-agnostic query shaping options for a repository query.
/// Abstracts ordering, pagination, and related-entity loading without leaking ORM concepts.
/// </summary>
/// <typeparam name="TEntity">The entity type being queried.</typeparam>
public interface IQueryOptions<TEntity>
{
    /// <summary>Gets the ordering function, or <see langword="null"/> for unordered results.</summary>
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? OrderBy { get; }

    /// <summary>Gets the number of records to skip (offset). Used for pagination.</summary>
    int? Skip { get; }

    /// <summary>Gets the maximum number of records to return. Used for pagination.</summary>
    int? Take { get; }

    /// <summary>
    /// Gets the collection of eager-loading paths.
    /// Each entry is a strongly-typed expression representing a navigation property chain.
    /// </summary>
    IReadOnlyList<IncludePath<TEntity>> Includes { get; }
}
