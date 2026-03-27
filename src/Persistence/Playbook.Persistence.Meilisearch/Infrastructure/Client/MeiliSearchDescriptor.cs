using System.Linq.Expressions;

using Meilisearch;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

/// <summary>
/// Provides a high-performance, fluent DSL (Domain Specific Language) for constructing 
/// type-safe Meilisearch queries. This descriptor orchestrates the complex mapping 
/// between C# expressions and Meilisearch's internal search parameters, including 
/// filtering, sorting, pagination, and hit highlighting.
/// </summary>
/// <typeparam name="T">The document model type used for type-safe property selection.</typeparam>
public sealed class MeiliSearchDescriptor<T>(string? searchTerm)
{
    private readonly SearchQuery _query = new() { Q = searchTerm };
    private readonly MeiliFilterBuilder<T> _filterBuilder = new();
    private readonly List<string> _sorts = [];

    /// <summary>
    /// Configures the filter criteria for the search query using a nested builder pattern.
    /// </summary>
    /// <param name="filterAction">A delegate to configure the <see cref="MeiliFilterBuilder{T}"/>.</param>
    /// <returns>The current <see cref="MeiliSearchDescriptor{T}"/> instance for method chaining.</returns>
    public MeiliSearchDescriptor<T> WithFilters(Action<MeiliFilterBuilder<T>> filterAction)
    {
        filterAction(_filterBuilder);
        return this;
    }

    /// <summary>
    /// Defines pagination boundaries for the search results.
    /// </summary>
    /// <param name="limit">The maximum number of documents to return.</param>
    /// <param name="offset">The number of documents to skip.</param>
    /// <returns>The current <see cref="MeiliSearchDescriptor{T}"/> instance.</returns>
    public MeiliSearchDescriptor<T> Paging(int? limit, int? offset)
    {
        _query.Limit = limit;
        _query.Offset = offset;
        return this;
    }

    /// <summary>
    /// Specifies a subset of attributes to retrieve from the search engine to reduce payload size.
    /// </summary>
    /// <param name="selectors">Expressions identifying the properties to include in the response.</param>
    /// <returns>The current <see cref="MeiliSearchDescriptor{T}"/> instance.</returns>
    public MeiliSearchDescriptor<T> Select(params Expression<Func<T, object?>>[] selectors)
    {
        // Resolves property names using the shared cache to avoid reflection overhead.
        _query.AttributesToRetrieve = selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName).ToList();
        return this;
    }

    /// <summary>
    /// Configures which attributes should be returned as facets in the search result.
    /// </summary>
    /// <remarks>
    /// Important: The specified attributes must be configured as 'Filterable' within the Meilisearch index settings.
    /// </remarks>
    /// <param name="selectors">Expressions identifying the properties to use as facets.</param>
    /// <returns>The current <see cref="MeiliSearchDescriptor{T}"/> instance.</returns>
    public MeiliSearchDescriptor<T> Facets(params Expression<Func<T, object?>>[] selectors)
    {
        _query.Facets = selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName).ToList();
        return this;
    }

    /// <summary>
    /// Enables and configures highlighting for specific attributes.
    /// Wraps matching search terms in HTML tags for UI display.
    /// </summary>
    /// <param name="selectors">Expressions identifying the properties to highlight.</param>
    /// <returns>The current <see cref="MeiliSearchDescriptor{T}"/> instance.</returns>
    public MeiliSearchDescriptor<T> Highlight(params Expression<Func<T, object?>>[] selectors)
    {
        _query.AttributesToHighlight = selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName).ToList();

        // Defaults to standard HTML mark tags for consistent styling across web frontends.
        _query.HighlightPreTag = "<mark>";
        _query.HighlightPostTag = "</mark>";
        return this;
    }

    /// <summary>
    /// Adds an ascending sort order to the query based on the specified property.
    /// </summary>
    /// <typeparam name="TValue">The type of the property being sorted.</typeparam>
    /// <param name="propertySelector">Expression identifying the sort property.</param>
    /// <returns>The current <see cref="MeiliSearchDescriptor{T}"/> instance.</returns>
    public MeiliSearchDescriptor<T> SortBy<TValue>(Expression<Func<T, TValue>> propertySelector)
    {
        _sorts.Add($"{MeiliFilterBuilder<T>.GetCachedPropertyName(propertySelector)}:asc");
        return this;
    }

    /// <summary>
    /// Adds a descending sort order to the query based on the specified property.
    /// </summary>
    /// <typeparam name="TValue">The type of the property being sorted.</typeparam>
    /// <param name="propertySelector">Expression identifying the sort property.</param>
    /// <returns>The current <see cref="MeiliSearchDescriptor{T}"/> instance.</returns>
    public MeiliSearchDescriptor<T> SortByDescending<TValue>(Expression<Func<T, TValue>> propertySelector)
    {
        _sorts.Add($"{MeiliFilterBuilder<T>.GetCachedPropertyName(propertySelector)}:desc");
        return this;
    }

    /// <summary>
    /// Finalizes and compiles the descriptor into a <see cref="SearchQuery"/> object 
    /// ready for transmission to the Meilisearch API.
    /// </summary>
    /// <returns>A fully configured <see cref="SearchQuery"/> instance.</returns>
    public SearchQuery Build()
    {
        // Consolidates filters and sort rules only at the final build stage to optimize string allocations.
        _query.Filter = _filterBuilder.Build();
        _query.Sort = _sorts.Count > 0 ? _sorts : null;
        return _query;
    }
}
