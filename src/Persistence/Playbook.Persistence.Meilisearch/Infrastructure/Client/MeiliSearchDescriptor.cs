using System.Linq.Expressions;

using Meilisearch;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

public sealed class MeiliSearchDescriptor<T>(string? searchTerm)
{
    private readonly SearchQuery _query = new() { Q = searchTerm };
    private readonly MeiliFilterBuilder<T> _filterBuilder = new();
    private readonly List<string> _sorts = [];

    public MeiliSearchDescriptor<T> WithFilters(Action<MeiliFilterBuilder<T>> filterAction)
    {
        filterAction(_filterBuilder);
        _query.Filter = _filterBuilder.Build();
        return this;
    }

    public MeiliSearchDescriptor<T> Paging(int limit, int offset)
    {
        _query.Limit = limit;
        _query.Offset = offset;
        return this;
    }

    public MeiliSearchDescriptor<T> Select(params Expression<Func<T, object?>>[] selectors)
    {
        _query.AttributesToRetrieve = [.. selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName)];
        return this;
    }

    public MeiliSearchDescriptor<T> Facets(params Expression<Func<T, object?>>[] selectors)
    {
        _query.Facets = [.. selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName)];
        return this;
    }

    public MeiliSearchDescriptor<T> Highlight(params Expression<Func<T, object?>>[] selectors)
    {
        _query.AttributesToHighlight = [.. selectors.Select(MeiliFilterBuilder<T>.GetCachedPropertyName)];
        _query.HighlightPreTag = "<mark>";
        _query.HighlightPostTag = "</mark>";
        return this;
    }

    public MeiliSearchDescriptor<T> SortBy<TValue>(Expression<Func<T, TValue>> propertySelector)
    {
        var fieldName = MeiliFilterBuilder<T>.GetCachedPropertyName(propertySelector);
        _sorts.Add($"{fieldName}:asc");
        return this;
    }

    public MeiliSearchDescriptor<T> SortByDescending<TValue>(Expression<Func<T, TValue>> propertySelector)
    {
        var fieldName = MeiliFilterBuilder<T>.GetCachedPropertyName(propertySelector);
        _sorts.Add($"{fieldName}:desc");
        return this;
    }

    public SearchQuery Build()
    {
        _query.Filter = _filterBuilder.Build();
        _query.Sort = _sorts.Count > 0 ? _sorts : null;
        return _query;
    }
}
