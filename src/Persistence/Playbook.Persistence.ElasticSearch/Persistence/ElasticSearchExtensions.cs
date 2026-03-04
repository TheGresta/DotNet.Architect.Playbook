using System.Linq.Expressions;
using System.Reflection;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Persistence;

/// <summary>
/// Provides high-level extension methods for <see cref="SearchRequestDescriptor{T}"/> to simplify complex query and sort building.
/// </summary>
internal static class ElasticSearchExtensions
{
    private const string DefaultNameBoost = "name^3";
    private const string DefaultDescriptionField = "description";
    private const string KeywordSuffix = ".keyword";

    /// <summary>
    /// Applies sorting logic to the search request based on a property expression.
    /// </summary>
    /// <typeparam name="T">The document type inheriting from <see cref="BaseDocument"/>.</typeparam>
    /// <param name="descriptor">The search request descriptor being built.</param>
    /// <param name="sortByExpression">A lambda expression identifying the property to sort by.</param>
    /// <param name="sortDescending">Indicates whether to sort in descending order.</param>
    /// <returns>The modified <see cref="SearchRequestDescriptor{T}"/>.</returns>
    /// <remarks>
    /// This method automatically appends the <c>.keyword</c> suffix to <see cref="string"/> properties. 
    /// This ensures sorting is performed on the exact, non-analyzed value rather than the tokenized text.
    /// </remarks>
    public static SearchRequestDescriptor<T> ApplySort<T>(
        this SearchRequestDescriptor<T> descriptor,
        Expression<Func<T, object>>? sortByExpression,
        bool sortDescending) where T : BaseDocument
    {
        if (sortByExpression is null) return descriptor;

        var member = GetMemberInfo(sortByExpression);
        if (member is null) return descriptor;

        var fieldName = member.Name.ToLowerInvariant();
        var isString = member switch
        {
            PropertyInfo pi => pi.PropertyType == typeof(string),
            FieldInfo fi => fi.FieldType == typeof(string),
            _ => false
        };

        // Only append .keyword for string fields to target the non-analyzed version
        var finalSortField = isString ? $"{fieldName}{KeywordSuffix}" : fieldName;

        return descriptor.Sort(sort => sort
            .Field(finalSortField, d => d
                .Order(sortDescending ? SortOrder.Desc : SortOrder.Asc)
                .UnmappedType(FieldType.Keyword)
            )
        );
    }

    /// <summary>
    /// Dynamically constructs a boolean query combining full-text search and exact-match filters.
    /// </summary>
    /// <typeparam name="T">The document type inheriting from <see cref="BaseDocument"/>.</typeparam>
    /// <param name="descriptor">The search request descriptor being built.</param>
    /// <param name="filters">A dictionary of field expressions and their associated values for filtering.</param>
    /// <param name="term">An optional search string for full-text multi-match queries.</param>
    /// <returns>The modified <see cref="SearchRequestDescriptor{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// If neither <paramref name="term"/> nor <paramref name="filters"/> are provided, the method defaults to a <c>match_all</c> query.
    /// </para>
    /// <para>
    /// Full-text search is applied via <c>MultiMatch</c> targeting the "name" (with a 3x boost) and "description" fields.
    /// Filters are applied as a <c>Filter</c> clause, which bypasses scoring and improves performance through caching.
    /// </para>
    /// </remarks>
    public static SearchRequestDescriptor<T> ApplyDynamicQuery<T>(
        this SearchRequestDescriptor<T> descriptor,
        Dictionary<Expression<Func<T, object>>, object> filters,
        string? term = null) where T : BaseDocument
    {
        return descriptor.Query(q =>
        {
            var hasTerm = !string.IsNullOrWhiteSpace(term);
            var hasFilters = filters is { Count: > 0 };

            if (!hasTerm && !hasFilters)
            {
                q.MatchAll(_ => { });
                return;
            }

            q.Bool(b =>
            {
                if (hasTerm)
                {
                    b.Must(m => m.MultiMatch(mm => mm
                        .Query(term!)
                        .Fields(Fields.FromStrings([DefaultNameBoost, DefaultDescriptionField]))
                    ));
                }

                if (hasFilters)
                {
                    ApplyFilterQueries(b, filters);
                }
            });
        });
    }

    #region Private Helpers

    /// <summary>
    /// Extracts <see cref="MemberInfo"/> from a variety of expression types.
    /// </summary>
    /// <param name="expression">The expression to parse.</param>
    /// <returns>The <see cref="MemberInfo"/> if found; otherwise, <see langword="null"/>.</returns>
    private static MemberInfo? GetMemberInfo(Expression expression) => expression switch
    {
        LambdaExpression le => GetMemberInfo(le.Body),
        UnaryExpression { Operand: MemberExpression me } => me.Member,
        MemberExpression me => me.Member,
        _ => null
    };

    /// <summary>
    /// Converts the filter dictionary into a collection of Elasticsearch <see cref="TermQuery"/> objects.
    /// </summary>
    /// <typeparam name="T">The document type inheriting from <see cref="BaseDocument"/>.</typeparam>
    /// <param name="boolDescriptor">The boolean query descriptor to which filters will be added.</param>
    /// <param name="filters">The dictionary containing field expressions and values.</param>
    private static void ApplyFilterQueries<T>(BoolQueryDescriptor<T> boolDescriptor, Dictionary<Expression<Func<T, object>>, object> filters)
        where T : BaseDocument
    {
        var filterQueries = filters
            .Where(x => x.Value is not null)
            .Select(x => (Query)new TermQuery(new Field(x.Key))
            {
                Value = x.Value.ToString() ?? string.Empty
            })
            .ToArray();

        if (filterQueries.Length > 0)
        {
            boolDescriptor.Filter(filterQueries);
        }
    }

    #endregion
}