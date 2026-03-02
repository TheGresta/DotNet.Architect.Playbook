using Microsoft.EntityFrameworkCore;
using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Persistence.Extensions;

internal static class PaginateExtensions
{
    public static async Task<Paginate<T>> ToPaginateAsync<T>(this IQueryable<T> source, int index, int size, CancellationToken cancellationToken)
    {
        index = Math.Max(0, index);
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

    public static Paginate<T> ToPaginate<T>(this IEnumerable<T> source, int index, int size)
    {
        var collection = source as ICollection<T>;
        int count = collection?.Count ?? source.Count();

        var items = source.Skip(index * size).Take(size).ToList();

        return new Paginate<T>(items, count, index, size);
    }
}
