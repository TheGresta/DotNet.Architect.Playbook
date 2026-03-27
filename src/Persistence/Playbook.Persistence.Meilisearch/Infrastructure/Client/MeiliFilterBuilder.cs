using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

/// <summary>
/// A high-performance, type-safe fluent builder for constructing Meilisearch filter strings.
/// This utility leverages expression trees to provide a refactor-friendly way to define 
/// search constraints while eliminating the overhead of repeated reflection through internal caching.
/// </summary>
/// <typeparam name="T">The document model type used to derive property names and attributes.</typeparam>
public sealed class MeiliFilterBuilder<T>
{
    /// <summary>
    /// Thread-safe cache to store the mapping between C# property names and their 
    /// corresponding Meilisearch/JSON attribute names.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> _propertyCache = new();
    private readonly List<string> _filters = [];
    private string _logicalOperator = " AND ";

    /// <summary>
    /// Adds an equality constraint to the filter. 
    /// Automatically ignores null or whitespace values to prevent malformed queries.
    /// </summary>
    /// <typeparam name="TValue">The type of the property being filtered.</typeparam>
    /// <param name="propertySelector">An expression identifying the property (e.g., x => x.Id).</param>
    /// <param name="value">The value to match against.</param>
    /// <returns>The current <see cref="MeiliFilterBuilder{T}"/> instance for method chaining.</returns>
    public MeiliFilterBuilder<T> WhereEquals<TValue>(Expression<Func<T, TValue>> propertySelector, TValue? value)
    {
        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s))) return this;

        _filters.Add($"{GetCachedPropertyName(propertySelector)} = {MeiliFormatter.Format(value)}");
        return this;
    }

    /// <summary>
    /// Adds an 'IN' constraint, allowing for matching against a collection of values.
    /// </summary>
    /// <typeparam name="TValue">The type of the elements in the collection.</typeparam>
    /// <param name="propertySelector">An expression identifying the property.</param>
    /// <param name="values">The collection of values to include in the filter.</param>
    /// <returns>The current <see cref="MeiliFilterBuilder{T}"/> instance for method chaining.</returns>
    public MeiliFilterBuilder<T> WhereIn<TValue>(Expression<Func<T, TValue>> propertySelector, IEnumerable<TValue>? values)
    {
        // Filters out null entries within the collection to ensure the resulting Meilisearch array is valid.
        var validValues = values?.Where(v => v is not null).Cast<object>().ToList();
        if (validValues is null || validValues.Count == 0) return this;

        var formattedItems = string.Join(", ", validValues.Select(MeiliFormatter.Format));
        _filters.Add($"{GetCachedPropertyName(propertySelector)} IN [{formattedItems}]");
        return this;
    }

    /// <summary>
    /// Adds a greater-than-or-equal-to (>=) comparison filter for numeric or comparable types.
    /// </summary>
    /// <typeparam name="TValue">The value type being compared.</typeparam>
    /// <param name="propertySelector">An expression identifying the property.</param>
    /// <param name="value">The threshold value.</param>
    /// <returns>The current <see cref="MeiliFilterBuilder{T}"/> instance for method chaining.</returns>
    public MeiliFilterBuilder<T> WhereGreaterThanOrEqual<TValue>(Expression<Func<T, TValue>> propertySelector, TValue? value)
        where TValue : struct, IComparable
    {
        if (!value.HasValue) return this;
        _filters.Add($"{GetCachedPropertyName(propertySelector)} >= {MeiliFormatter.Format(value.Value)}");
        return this;
    }

    /// <summary>
    /// Adds a less-than-or-equal-to (<=) comparison filter for numeric or comparable types.
    /// </summary>
    /// <typeparam name="TValue">The value type being compared.</typeparam>
    /// <param name="propertySelector">An expression identifying the property.</param>
    /// <param name="value">The threshold value.</param>
    /// <returns>The current <see cref="MeiliFilterBuilder{T}"/> instance for method chaining.</returns>
    public MeiliFilterBuilder<T> WhereLessThanOrEqual<TValue>(Expression<Func<T, TValue>> propertySelector, TValue? value)
        where TValue : struct, IComparable
    {
        if (!value.HasValue) return this;
        _filters.Add($"{GetCachedPropertyName(propertySelector)} <= {MeiliFormatter.Format(value.Value)}");
        return this;
    }

    /// <summary>
    /// Initiates a nested logical OR grouping.
    /// </summary>
    /// <param name="action">A delegate to configure the nested builder.</param>
    /// <returns>The current <see cref="MeiliFilterBuilder{T}"/> instance for method chaining.</returns>
    public MeiliFilterBuilder<T> Or(Action<MeiliFilterBuilder<T>> action)
    {
        // Creates a scope-specific builder that defaults to OR joining for its internal filter list.
        var subBuilder = new MeiliFilterBuilder<T> { _logicalOperator = " OR " };
        action(subBuilder);
        var result = subBuilder.Build();

        if (!string.IsNullOrEmpty(result))
            _filters.Add($"({result})");

        return this;
    }

    /// <summary>
    /// Compiles the accumulated filter segments into a single Meilisearch-compatible filter string.
    /// </summary>
    /// <returns>A formatted filter string, or null if no filters were applied.</returns>
    public string? Build() => _filters.Count switch
    {
        0 => null,
        1 => _filters[0],
        _ => string.Join(_logicalOperator, _filters)
    };

    /// <summary>
    /// Resolves the property name from an expression, checking the cache first.
    /// </summary>
    public static string GetCachedPropertyName<TValue>(Expression<Func<T, TValue>> expression)
    {
        var memberInfo = GetMemberInfo(expression);
        return GetCachedPropertyName(memberInfo);
    }

    /// <summary>
    /// Retrieves the resolved JSON property name for a given <see cref="MemberInfo"/>.
    /// Priorities: [JsonPropertyName] attribute -> Lowercase property name.
    /// </summary>
    public static string GetCachedPropertyName(MemberInfo memberInfo)
    {
        var cacheKey = $"{typeof(T).FullName}.{memberInfo.Name}";

        // Atomic operation to retrieve or reflect-and-store the property mapping.
        return _propertyCache.GetOrAdd(cacheKey, _ =>
        {
            var attribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            return attribute?.Name ?? JsonNamingPolicy.SnakeCaseLower.ConvertName(memberInfo.Name);
        });
    }

    /// <summary>
    /// Unwraps the Expression tree to extract the underlying MemberInfo.
    /// Handles both direct property access and boxing conversions (UnaryExpression).
    /// </summary>
    private static MemberInfo GetMemberInfo<TValue>(Expression<Func<T, TValue>> expression) => expression.Body switch
    {
        MemberExpression m => m.Member,
        UnaryExpression { Operand: MemberExpression m } => m.Member,
        _ => throw new ArgumentException("Invalid property selector")
    };
}
