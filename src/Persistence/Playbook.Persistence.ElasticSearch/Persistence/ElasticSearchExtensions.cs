using System.Collections.Concurrent;
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
    /// <summary>
    /// Caches the reflected field list per document type to avoid repeated reflection overhead.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, string[]> SearchFieldCache = [];

    /// <summary>
    /// The suffix used to target non-analyzed string fields in Elasticsearch.
    /// </summary>
    private const string KeywordSuffix = ".keyword";

    /// <summary>
    /// Applies sorting logic to the search request based on a property expression.
    /// </summary>
    /// <typeparam name="T">The document type inheriting from <see cref="BaseDocument"/>.</typeparam>
    /// <param name="descriptor">The search request descriptor being built.</param>
    /// <param name="sortByExpression">A lambda expression identifying the property to sort by. If <see langword="null"/>, no sort is applied.</param>
    /// <param name="sortDescending">Indicates whether to sort in descending order (<see langword="true"/>) or ascending order (<see langword="false"/>).</param>
    /// <returns>The modified <see cref="SearchRequestDescriptor{T}"/> instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method automatically converts C# PascalCase property names to camelCase to match the default serialization behavior 
    /// of the <c>Elastic.Clients.Elasticsearch</c> v8 client.
    /// </para>
    /// <para>
    /// For <see cref="string"/> properties, the method appends the <c>.keyword</c> suffix. 
    /// This ensures sorting is performed on the exact, non-analyzed value rather than the tokenized text.
    /// </para>
    /// <code language="csharp">
    /// // Example: Sorting by Name descending
    /// searchDescriptor.ApplySort(x => x.Name, true);
    /// </code>
    /// </remarks>
    public static SearchRequestDescriptor<T> ApplySort<T>(
        this SearchRequestDescriptor<T> descriptor,
        Expression<Func<T, object>>? sortByExpression,
        bool sortDescending) where T : BaseDocument
    {
        if (sortByExpression is null) return descriptor;

        var member = GetMemberInfo(sortByExpression);
        if (member is null) return descriptor;

        // Use camelCase to match the Elastic.Clients.Elasticsearch v8 default serialisation
        var fieldName = char.ToLowerInvariant(member.Name[0]) + member.Name[1..];

        var isString = member switch
        {
            PropertyInfo pi => (Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType) == typeof(string),
            FieldInfo fi => (Nullable.GetUnderlyingType(fi.FieldType) ?? fi.FieldType) == typeof(string),
            _ => false
        };

        // Only append .keyword for string fields to target the non-analysed value
        var finalSortField = isString ? $"{fieldName}{KeywordSuffix}" : fieldName;

        // Use the type-appropriate UnmappedType so Elasticsearch handles unmapped fields correctly
        var unmappedType = ResolveFieldType(member);

        return descriptor.Sort(sort => sort
            .Field(finalSortField, d => d
                .Order(sortDescending ? SortOrder.Desc : SortOrder.Asc)
                .UnmappedType(unmappedType)
            )
        );
    }

    /// <summary>
    /// Dynamically constructs a boolean query combining full-text search and exact-match filters.
    /// </summary>
    /// <typeparam name="T">The document type inheriting from <see cref="BaseDocument"/>.</typeparam>
    /// <param name="descriptor">The search request descriptor being built.</param>
    /// <param name="filters">A <see cref="Dictionary{TKey, TValue}"/> of field expressions and their associated values for filtering. Null values are ignored.</param>
    /// <param name="term">An optional search string for full-text multi-match queries. If <see langword="null"/> or whitespace, full-text search is skipped.</param>
    /// <returns>The modified <see cref="SearchRequestDescriptor{T}"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// If neither <paramref name="term"/> nor <paramref name="filters"/> are provided, the method defaults to a <c>match_all</c> query.
    /// </para>
    /// <para>
    /// Full-text search fields are resolved via reflection and cached in <see cref="SearchFieldCache"/>. 
    /// These fields must be decorated with the <c>SearchableFieldAttribute</c>.
    /// </para>
    /// <para>
    /// Filters are applied as a <c>Filter</c> clause, which bypasses relevance scoring and improves performance through caching.
    /// </para>
    /// </remarks>
    public static SearchRequestDescriptor<T> ApplyDynamicQuery<T>(
        this SearchRequestDescriptor<T> descriptor,
        Dictionary<Expression<Func<T, object>>, object> filters,
        string? term = null) where T : BaseDocument
    {
        if (string.IsNullOrWhiteSpace(term) && filters.Count == 0)
            return descriptor.Query(q => q.MatchAll(new MatchAllQuery()));

        var searchFields = SearchFieldCache.GetOrAdd(typeof(T), ResolveSearchFields);

        if (!string.IsNullOrWhiteSpace(term) && searchFields.Length == 0)
            throw new InvalidOperationException(
                $"A search term was provided but no [SearchableField] attributes were found on '{typeof(T).Name}'. " +
                $"Decorate at least one property with [SearchableField] to enable full-text search.");

        return descriptor.Query(q => q.Bool(b =>
        {
            if (!string.IsNullOrWhiteSpace(term) && searchFields.Length > 0)
            {
                b.Must(m => m.MultiMatch(mm => mm
                    .Query(term)
                    .Fields(searchFields)));
            }

            ApplyFilterQueries(b, filters);
        }));
    }

    #region Private Helpers

    /// <summary>
    /// Extracts <see cref="MemberInfo"/> from a variety of expression types, including boxing and unary conversions.
    /// </summary>
    /// <param name="expression">The expression to parse.</param>
    /// <returns>The <see cref="MemberInfo"/> if the expression represents a field or property; otherwise, <see langword="null"/>.</returns>
    private static MemberInfo? GetMemberInfo(Expression expression) => expression switch
    {
        LambdaExpression le => GetMemberInfo(le.Body),
        UnaryExpression { Operand: MemberExpression me } => me.Member,
        MemberExpression me => me.Member,
        _ => null
    };

    /// <summary>
    /// Converts the filter dictionary into a collection of Elasticsearch <see cref="TermQuery"/> objects and applies them to the descriptor.
    /// </summary>
    /// <typeparam name="T">The document type inheriting from <see cref="BaseDocument"/>.</typeparam>
    /// <param name="boolDescriptor">The boolean query descriptor to which filters will be added.</param>
    /// <param name="filters">The dictionary containing field expressions and values used to build <see cref="TermQuery"/> clauses.</param>
    private static void ApplyFilterQueries<T>(BoolQueryDescriptor<T> boolDescriptor, Dictionary<Expression<Func<T, object>>, object> filters)
        where T : BaseDocument
    {
        var filterQueries = filters
            .Where(x => x.Value is not null)
            .Select(x =>
            {
                var member = GetMemberInfo(x.Key)
                    ?? throw new ArgumentException(
                        $"Filter expression '{x.Key}' must target a direct field/property access.",
                            nameof(filters));

                var rawFieldName = char.ToLowerInvariant(member.Name[0]) + member.Name[1..];

                // Mirror the same string-detection logic used in ApplySort
                var isStringField = member switch
                {
                    PropertyInfo pi => pi.PropertyType == typeof(string),
                    FieldInfo fi => fi.FieldType == typeof(string),
                    _ => false
                };

                // Append .keyword only for string fields (exact, non-analyzed matching)
                var fieldName = isStringField
                    ? $"{rawFieldName}{KeywordSuffix}"
                    : rawFieldName;

                // Preserve the native type so Elasticsearch receives the correct JSON value
                FieldValue fieldValue = x.Value switch
                {
                    bool b => b,
                    int i => (long)i,
                    long l => l,
                    float f => (double)f,
                    double d => d,
                    decimal dec => (double)dec,
                    string s => s,
                    Guid g => g.ToString(),
                    Enum e => e.ToString(),
                    _ => throw new NotSupportedException(
                        $"Unsupported filter value type '{x.Value.GetType().FullName}' for expression '{x.Key}'.")
                };

                return (Query)new TermQuery(fieldName) { Value = fieldValue };
            })
            .ToArray();

        if (filterQueries.Length > 0)
        {
            boolDescriptor.Filter(filterQueries);
        }
    }

    /// <summary>
    /// Uses reflection to identify properties decorated with <c>SearchableFieldAttribute</c> and formats them for Elasticsearch.
    /// </summary>
    /// <param name="documentType">The <see cref="Type"/> of the document to inspect.</param>
    /// <returns>An array of field names, potentially including boost factors (e.g., "fieldName^3").</returns>
    private static string[] ResolveSearchFields(Type documentType)
    {
        return [.. documentType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.IsDefined(typeof(SearchableFieldAttribute), inherit: true))
            .Select(p =>
            {
                var attr = p.GetCustomAttribute<SearchableFieldAttribute>()!;
                var fieldName = attr.FieldName ?? (char.ToLowerInvariant(p.Name[0]) + p.Name[1..]);
                return attr.Boost > 1.0 ? $"{fieldName}^{attr.Boost}" : fieldName;
            })];
    }

    /// <summary>
    /// Maps a <see cref="MemberInfo"/> to its corresponding Elasticsearch <see cref="FieldType"/>.
    /// </summary>
    /// <param name="member">The property or field to resolve.</param>
    /// <returns>The <see cref="FieldType"/> to use for the <c>UnmappedType</c> setting in queries.</returns>
    private static FieldType ResolveFieldType(MemberInfo member)
    {
        var clrType = member switch
        {
            PropertyInfo pi => Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType,
            FieldInfo fi => Nullable.GetUnderlyingType(fi.FieldType) ?? fi.FieldType,
            _ => typeof(object)
        };

        return clrType switch
        {
            _ when clrType == typeof(string) => FieldType.Keyword,
            _ when clrType == typeof(int) => FieldType.Integer,
            _ when clrType == typeof(long) => FieldType.Long,
            _ when clrType == typeof(float) => FieldType.Float,
            _ when clrType == typeof(double) => FieldType.Double,
            _ when clrType == typeof(decimal) => FieldType.ScaledFloat,
            _ when clrType == typeof(DateTime) => FieldType.Date,
            _ when clrType == typeof(DateTimeOffset) => FieldType.Date,
            _ when clrType == typeof(bool) => FieldType.Boolean,
            _ => FieldType.Keyword
        };
    }

    #endregion
}