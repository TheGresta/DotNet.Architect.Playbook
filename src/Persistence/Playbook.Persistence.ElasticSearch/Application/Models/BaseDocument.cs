namespace Playbook.Persistence.ElasticSearch.Application.Models;

/// <summary>
/// Provides a base abstraction for all documents stored in the search index.
/// </summary>
/// <remarks>
/// This class ensures type safety and a consistent identity schema for Elasticsearch document models.
/// </remarks>
public abstract class BaseDocument
{
    /// <summary>
    /// Gets the unique identifier for the document.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> representing the unique ID. Defaults to a new <see cref="Guid"/> string.
    /// </value>
    public string Id { get; init; } = Guid.NewGuid().ToString();
}