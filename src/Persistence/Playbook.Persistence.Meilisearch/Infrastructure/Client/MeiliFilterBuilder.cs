using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

public sealed class MeiliFilterBuilder<T>
{
    // High-Performance Cache to avoid Reflection overhead on every search
    private static readonly ConcurrentDictionary<string, string> _propertyCache = new();
    private readonly List<string> _filters = [];

    public MeiliFilterBuilder<T> WhereEquals<TValue>(Expression<Func<T, TValue>> propertySelector, TValue? value)
    {
        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s))) return this;

        var fieldName = GetCachedPropertyName(propertySelector);
        var formattedValue = value is string ? $"\"{value}\"" : value.ToString()?.ToLowerInvariant();

        _filters.Add($"{fieldName} = {formattedValue}");
        return this;
    }

    public MeiliFilterBuilder<T> WhereGreaterThanOrEqual<TValue>(Expression<Func<T, TValue>> propertySelector, TValue? value)
        where TValue : struct
    {
        if (!value.HasValue) return this;

        var fieldName = GetCachedPropertyName(propertySelector);
        _filters.Add($"{fieldName} >= {value}");
        return this;
    }

    public MeiliFilterBuilder<T> WhereLessThanOrEqual<TValue>(Expression<Func<T, TValue>> propertySelector, TValue? value)
        where TValue : struct
    {
        if (!value.HasValue) return this;

        var fieldName = GetCachedPropertyName(propertySelector);
        _filters.Add($"{fieldName} <= {value}");
        return this;
    }

    public string? Build() => _filters.Count > 0 ? string.Join(" AND ", _filters) : null;

    public static string GetCachedPropertyName<TValue>(Expression<Func<T, TValue>> expression)
    {
        var memberInfo = GetMemberInfo(expression);
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
        UnaryExpression u when u.Operand is MemberExpression m => m.Member,
        _ => throw new ArgumentException("Invalid expression. Must be a property selector.")
    };
}
