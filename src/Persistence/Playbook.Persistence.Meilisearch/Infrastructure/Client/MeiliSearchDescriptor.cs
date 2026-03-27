using System.Linq.Expressions;

using Meilisearch;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

/// <summary>
/// A high-performance fluent descriptor for building type-safe Meilisearch queries.
/// Handles the orchestration of filters, sorting, paging, and highlighting.
/// </summary>
public sealed class MeiliSearchDescriptor<T>(string? searchTerm)
{
    private readonly SearchQuery _query = new() { Q = searchTerm };
    private readonly MeiliFilterBuilder<T> _filterBuilder = new();
    private readonly List<string> _sorts = [];

    public MeiliSearchDescriptor<T> WithFilters(Action<MeiliFilterBuilder<T>> filterAction)
    {
        filterAction(_filterBuilder);
        return this;
    }

    public MeiliSearchDescriptor<T> Paging(int? limit, int? offset)
    {
        _query.Limit = limit;
        _query.Offset = offset;
        return this;
    }

    public MeiliSearchDescriptor<T> Select(params Expression<Func<T, object?>>[] selectors)
    {
        _query.AttributesToRetrieve = selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName).ToList();
        return this;
    }

    /// <summary>
    /// Configures which attributes should be returned as facets.
    /// Note: Ensure these are set as 'Filterable' in index settings.
    /// </summary>
    public MeiliSearchDescriptor<T> Facets(params Expression<Func<T, object?>>[] selectors)
    {
        _query.Facets = selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName).ToList();
        return this;
    }

    public MeiliSearchDescriptor<T> Highlight(params Expression<Func<T, object?>>[] selectors)
    {
        _query.AttributesToHighlight = selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName).ToList();
        _query.HighlightPreTag = "<mark>";
        _query.HighlightPostTag = "</mark>";
        return this;
    }

    public MeiliSearchDescriptor<T> SortBy<TValue>(Expression<Func<T, TValue>> propertySelector)
    {
        _sorts.Add($"{MeiliFilterBuilder<T>.GetCachedPropertyName(propertySelector)}:asc");
        return this;
    }

    public MeiliSearchDescriptor<T> SortByDescending<TValue>(Expression<Func<T, TValue>> propertySelector)
    {
        _sorts.Add($"{MeiliFilterBuilder<T>.GetCachedPropertyName(propertySelector)}:desc");
        return this;
    }

    /// <summary>
    /// Finalizes the query object. 
    /// </summary>
    public SearchQuery Build()
    {
        _query.Filter = _filterBuilder.Build();
        _query.Sort = _sorts.Count > 0 ? _sorts : null;
        return _query;
    }
}
