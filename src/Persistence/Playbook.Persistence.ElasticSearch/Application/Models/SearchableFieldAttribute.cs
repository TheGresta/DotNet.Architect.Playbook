namespace Playbook.Persistence.ElasticSearch.Application.Models;

/// <summary>
/// Marks a property as a full-text searchable field for Elasticsearch multi-match queries.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SearchableFieldAttribute : Attribute
{
    /// <summary>Optional boost factor (e.g. 3 → "name^3"). Defaults to 1 (no boost).</summary>
    public double Boost { get; init; } = 1.0;

    /// <summary>Overrides the Elasticsearch field name. Defaults to the lower-cased property name.</summary>
    public string? FieldName { get; init; }
}
