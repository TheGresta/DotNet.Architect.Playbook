namespace Playbook.Persistence.EntityFramework.Domain.Base;

/// <summary>
/// Represents a generic class for paginating a collection of items, 
/// providing information about the current page, the total number of items, and the items themselves.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class Paginate<T>(IEnumerable<T> items, int count, int index, int size)
{
    public int Index { get; } = index;
    public int Size { get; } = size;
    public int Count { get; } = count;
    public int Pages => Size > 0 ? (int)Math.Ceiling(Count / (double)Size) : 0;
    public List<T> Items { get; } = [.. items];
    public bool HasPrevious => Index > 0;
    public bool HasNext => Index + 1 < Pages;
}
