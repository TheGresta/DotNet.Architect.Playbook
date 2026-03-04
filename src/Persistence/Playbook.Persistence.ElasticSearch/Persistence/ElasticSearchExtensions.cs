using System.Linq.Expressions;
using System.Reflection;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Playbook.Persistence.ElasticSearch.Application.Models;

namespace Playbook.Persistence.ElasticSearch.Persistence;

internal static class ElasticSearchExtensions
{
    private const string DefaultNameBoost = "name^3";
    private const string DefaultDescriptionField = "description";
    private const string KeywordSuffix = ".keyword";

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

    private static MemberInfo? GetMemberInfo(Expression expression) => expression switch
    {
        LambdaExpression le => GetMemberInfo(le.Body),
        UnaryExpression { Operand: MemberExpression me } => me.Member,
        MemberExpression me => me.Member,
        _ => null
    };

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