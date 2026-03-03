namespace Playbook.Persistence.EntityFramework.Domain.Base;

/// <summary>
/// Represents a paginated container for a collection of items, providing metadata for navigation and sizing.
/// </summary>
/// <typeparam name="T">The type of the elements contained in the paginated list.</typeparam>
/// <param name="items">The collection of items for the current page.</param>
/// <param name="count">The total number of records available across all pages.</param>
/// <param name="index">The zero-based index of the current page.</param>
/// <param name="size">The maximum number of items allowed per page.</param>
public class Paginate<T>(IEnumerable<T> items, int count, int index, int size)
{
    /// <summary>
    /// Gets the zero-based index of the current page.
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int Size { get; } = size;

    /// <summary>
    /// Gets the total number of items available across the entire data set.
    /// </summary>
    public int Count { get; } = count;

    /// <summary>
    /// Gets the total number of pages calculated based on <see cref="Count"/> and <see cref="Size"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> representing the total page count. Returns 0 if <see cref="Size"/> is 0.
    /// </value>
    public int Pages => Size > 0 ? (int)Math.Ceiling(Count / (double)Size) : 0;

    /// <summary>
    /// Gets the collection of items contained in the current page.
    /// </summary>
    public List<T> Items { get; } = [.. items];

    /// <summary>
    /// Gets a value indicating whether there is a page prior to the current <see cref="Index"/>.
    /// </summary>
    public bool HasPrevious => Index > 0;

    /// <summary>
    /// Gets a value indicating whether there is a page following the current <see cref="Index"/>.
    /// </summary>
    public bool HasNext => Index + 1 < Pages;
}
