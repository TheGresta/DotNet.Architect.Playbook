using System.Linq.Expressions;

namespace Playbook.Persistence.ElasticSearch.Application.Models;

/// <summary>
/// Encapsulates the parameters required to perform a complex search query against the index.
/// </summary>
/// <typeparam name="T">The type of document to search, constrained to <see cref="BaseDocument"/>.</typeparam>
public class SearchQuery<T> where T : BaseDocument
{
    private int page = 1;
    private int pageSize = 10;

    /// <summary>
    /// Gets or sets the full-text search term.
    /// </summary>
    public string? Term { get; set; }

    /// <summary>
    /// Gets or sets the current page number. Defaults to 1.
    /// </summary>
    public int Page
    {
        get => page;
        set => page = value< 1 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the maximum number of records to return in a single page. Defaults to 10.
    /// </summary>
    public int PageSize
    {
        get => pageSize;
        set => pageSize = value< 1 ? 10 : value;
    }

    /// <summary>
    /// Gets or sets the expression used to determine the field by which results are ordered.
    /// </summary>
    public Expression<Func<T, object>>? SortByExpression { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the results should be sorted in descending order.
    /// </summary>
    public bool SortDescending { get; set; }

    /// <summary>
    /// Gets or sets a collection of filters to apply to the query, mapped by field expressions.
    /// </summary>
    public Dictionary<Expression<Func<T, object>>, object> Filters { get; set; } = [];

    /// <summary>
    /// Gets the number of records to skip based on the <see cref="Page"/> and <see cref="PageSize"/>.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}