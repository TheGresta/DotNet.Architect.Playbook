using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

/// <summary>
/// A high-performance, type-safe builder for Meilisearch filter strings.
/// Uses internal caching to eliminate reflection overhead during query construction.
/// </summary>
public sealed class MeiliFilterBuilder<T>
{
    private static readonly ConcurrentDictionary<string, string> _propertyCache = new();
    private readonly List<string> _filters = [];
    private string _logicalOperator = " AND ";

    public MeiliFilterBuilder<T> WhereEquals<TValue>(Expression<Func<T, TValue>> propertySelector, TValue? value)
    {
        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s))) return this;

        _filters.Add($"{GetCachedPropertyName(propertySelector)} = {MeiliFormatter.Format(value)}");
        return this;
    }

    public MeiliFilterBuilder<T> WhereIn<TValue>(Expression<Func<T, TValue>> propertySelector, IEnumerable<TValue>? values)
    {
        var validValues = values?.Where(v => v is not null).Cast<object>().ToList();
        if (validValues is null || validValues.Count == 0) return this;

        var formattedItems = string.Join(", ", validValues.Select(MeiliFormatter.Format));
        _filters.Add($"{GetCachedPropertyName(propertySelector)} IN [{formattedItems}]");
        return this;
    }

    public MeiliFilterBuilder<T> WhereGreaterThanOrEqual<TValue>(Expression<Func<T, TValue>> propertySelector, TValue? value)
        where TValue : struct, IComparable
    {
        if (!value.HasValue) return this;
        _filters.Add($"{GetCachedPropertyName(propertySelector)} >= {MeiliFormatter.Format(value.Value)}");
        return this;
    }

    public MeiliFilterBuilder<T> Or(Action<MeiliFilterBuilder<T>> action)
    {
        var subBuilder = new MeiliFilterBuilder<T> { _logicalOperator = " OR " };
        action(subBuilder);
        var result = subBuilder.Build();

        if (!string.IsNullOrEmpty(result))
            _filters.Add($"({result})");

        return this;
    }

    public string? Build() => _filters.Count switch
    {
        0 => null,
        1 => _filters[0],
        _ => string.Join(_logicalOperator, _filters)
    };

    public static string GetCachedPropertyName<TValue>(Expression<Func<T, TValue>> expression)
    {
        var memberInfo = GetMemberInfo(expression);
        return GetCachedPropertyName(memberInfo);
    }

    public static string GetCachedPropertyName(MemberInfo memberInfo)
    {
        var cacheKey = $"{typeof(T).FullName}.{memberInfo.Name}";

        return _propertyCache.GetOrAdd(cacheKey, _ =>
        {
            var attribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            return attribute?.Name ?? memberInfo.Name.ToLowerInvariant();
        });
    }

    private static MemberInfo GetMemberInfo<TValue>(Expression<Func<T, TValue>> expression) => expression.Body switch
    {
        MemberExpression m => m.Member,
        UnaryExpression { Operand: MemberExpression m } => m.Member,
        _ => throw new ArgumentException("Invalid property selector")
    };
}
