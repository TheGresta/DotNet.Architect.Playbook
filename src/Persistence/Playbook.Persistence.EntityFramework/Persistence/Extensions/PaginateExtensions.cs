using Microsoft.EntityFrameworkCore;
using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Persistence.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> and <see cref="IEnumerable{T}"/> 
/// to facilitate asynchronous and synchronous pagination.
/// </summary>
internal static class PaginateExtensions
{
    /// <summary>
    /// Asynchronously creates a <see cref="Paginate{T}"/> from an <see cref="IQueryable{T}"/> source.
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="source">The <see cref="IQueryable{T}"/> to create the paginated list from.</param>
    /// <param name="index">The zero-based index of the page to retrieve.</param>
    /// <param name="size">The maximum number of items to include in the page.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a 
    /// <see cref="Paginate{T}"/> object that contains a subset of the data and pagination metadata.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Logic:</b>
    /// 1. <b>Validation:</b> Ensures <paramref name="index"/> is at least 0 and <paramref name="size"/> is between 1 and 100.<br/>
    /// 2. <b>Count:</b> Executes a <c>COUNT</c> query to determine the total record count for metadata.<br/>
    /// 3. <b>Short-circuit:</b> If count is 0, returns an empty <see cref="Paginate{T}"/> immediately.<br/>
    /// 4. <b>Slice:</b> Uses <see cref="Queryable.Skip{TSource}"/> and <see cref="Queryable.Take{TSource}"/> to fetch only the relevant data window.
    /// </para>
    /// </remarks>
    public static async Task<Paginate<T>> ToPaginateAsync<T>(this IQueryable<T> source, int index, int size, CancellationToken cancellationToken)
    {
        // Guard against negative indices
        index = Math.Max(0, index);

        // Guard against zero/negative size or "scraping" attacks by capping size at 100
        size = Math.Max(1, Math.Min(size, 100));

        int count = await source.CountAsync(cancellationToken);

        if (count == 0)
            return new Paginate<T>([], 0, index, size);

        List<T> items = await source
            .Skip(index * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new Paginate<T>(items, count, index, size);
    }

    /// <summary>
    /// Synchronously creates a <see cref="Paginate{T}"/> from an in-memory <see cref="IEnumerable{T}"/> source.
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to paginate.</param>
    /// <param name="index">The zero-based index of the page.</param>
    /// <param name="size">The size of the page.</param>
    /// <returns>A <see cref="Paginate{T}"/> containing the slice of data.</returns>
    /// <remarks>
    /// This method attempts to optimize by checking if the source implements <see cref="ICollection{T}"/> 
    /// before defaulting to an exhaustive enumeration for the count.
    /// </remarks>
    public static Paginate<T> ToPaginate<T>(this IEnumerable<T> source, int index, int size)
    {
        var collection = source as ICollection<T>;
        int count = collection?.Count ?? source.Count();

        var items = source.Skip(index * size).Take(size).ToList();

        return new Paginate<T>(items, count, index, size);
    }
}
