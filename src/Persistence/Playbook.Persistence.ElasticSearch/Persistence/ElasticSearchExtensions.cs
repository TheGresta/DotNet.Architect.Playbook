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

    public static SearchRequestDescriptor<T> ApplySort<T>(this SearchRequestDescriptor<T> descriptor, Expression<Func<T, object>>? sortByExpression, bool sortDescending)
        where T : BaseDocument
    {
        if (sortByExpression == null) return descriptor;

        MemberInfo? member = null;

        if (sortByExpression.Body is MemberExpression me)
            member = me.Member;
        else if (sortByExpression.Body is UnaryExpression ue && ue.Operand is MemberExpression ume)
            member = ume.Member;

        if (member == null) return descriptor;

        string fieldName = member.Name.ToLower();
        bool isString = false;

        if (member is PropertyInfo pi)
        {
            isString = pi.PropertyType == typeof(string);
        }
        else if (member is FieldInfo fi)
        {
            isString = fi.FieldType == typeof(string);
        }

        // Apply .keyword suffix only for strings
        string finalSortField = isString ? $"{fieldName}.keyword" : fieldName;

        return descriptor.Sort(sort => sort
            .Field(finalSortField, d => d
                .Order(sortDescending ? SortOrder.Desc : SortOrder.Asc)
                .UnmappedType(FieldType.Keyword)
            )
        );
    }

    public static SearchRequestDescriptor<T> ApplyDynamicQuery<T>(this SearchRequestDescriptor<T> descriptor, Dictionary<Expression<Func<T, object>>, object> filters, string? term = null)
        where T : BaseDocument
    {
        return descriptor.Query(q =>
        {
            bool hasTerm = !string.IsNullOrWhiteSpace(term);
            bool hasFilters = filters.Count > default(int);

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
                    var filterQueries = filters
                        .Where(x => x.Value != null)
                        .Select(x => Query.Term(new TermQuery(new Field(x.Key))
                        {
                            Value = x.Value.ToString() ?? string.Empty
                        }))
                        .ToArray();

                    if (filterQueries.Length > 0)
                    {
                        b.Filter(filterQueries);
                    }
                }
            });
        });
    }
}
