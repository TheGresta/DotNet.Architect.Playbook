using MongoDB.Driver.Linq;

using Playbook.Persistence.MongoDB.Domain;

namespace Playbook.Persistence.MongoDB.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> to facilitate server-side pagination.
/// </summary>
internal static class PaginateExtensions
{
    /// <summary>
    /// Asynchronously converts an <see cref="IQueryable{T}"/> source into a paginated result.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source collection.</typeparam>
    /// <param name="source">The queryable data source, typically a MongoDB collection query.</param>
    /// <param name="index">The zero-based page index to retrieve.</param>
    /// <param name="size">The maximum number of items to include in a single page.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the asynchronous operations to complete. The default is <see langword="default"/>.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing a <see cref="Paginate{T}"/> 
    /// object populated with the total count and the current page items.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method executes two separate queries on the MongoDB server:
    /// <list type="number">
    /// <item>A count operation to determine the total number of documents matching the query.</item>
    /// <item>A skip/take operation to retrieve only the subset of documents for the specified page.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: The <see cref="Paginate{T}.Pages"/> property is automatically calculated by the model 
    /// based on the <paramref name="size"/> and the total count.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="source"/> is null.</exception>
    public static async Task<Paginate<T>> ToPaginateAsync<T>(
        this IQueryable<T> source,
        int index,
        int size,
        CancellationToken ct = default)
    {
        // This execution happens ON the MongoDB Server
        var count = await source.CountAsync(ct);
        List<T> items = await source.Skip(index * size).Take(size).ToListAsync(ct);

        return new Paginate<T>
        {
            Index = index,
            Size = size,
            Count = count,
            Items = items
            // Pages is calculated automatically in our refined Domain model!
        };
    }
}
