namespace Playbook.Persistence.MongoDB.Domain;

/// <summary>
/// Represents a paginated container for a collection of items of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the elements contained in the paginated list.</typeparam>
public class Paginate<T>
{
    /// <summary>
    /// Gets or sets the zero-based index of the current page.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the total number of items available across all pages.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets the total number of pages based on the <see cref="Count"/> and <see cref="Size"/>.
    /// </summary>
    /// <value>The calculated number of pages, rounded up to the nearest integer.</value>
    public int Pages => (int)Math.Ceiling(Count / (double)Size);

    /// <summary>
    /// Gets or sets the collection of items for the current page.
    /// </summary>
    /// <value>A <see cref="List{T}"/> containing the items in the current page subset.</value>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether there is a previous page available.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Index"/> is greater than 0; otherwise, <see langword="false"/>.</value>
    public bool HasPrevious => Index > 0;

    /// <summary>
    /// Gets a value indicating whether there is a next page available.
    /// </summary>
    /// <value><see langword="true"/> if the current <see cref="Index"/> plus one is less than <see cref="Pages"/>; otherwise, <see langword="false"/>.</value>
    public bool HasNext => Index + 1 < Pages;
}
