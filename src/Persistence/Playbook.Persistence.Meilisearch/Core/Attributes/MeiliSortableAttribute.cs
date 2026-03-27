namespace Playbook.Persistence.Meilisearch.Core.Attributes;

/// <summary>
/// A decorative metadata attribute used to mark a document property as "Sortable" 
/// within the Meilisearch engine settings.
/// </summary>
/// <remarks>
/// When applied to a property in a document model (e.g., Price, CreatedAt), 
/// the <see cref="MeiliIndexConfiguration{T}"/> will detect this attribute via reflection 
/// and update the index settings to allow <c>sort</c> operations. 
/// Without this attribute, Meilisearch will reject queries attempting to order 
/// by the specific property.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class MeiliSortableAttribute : Attribute
{
    // This attribute serves as a metadata marker for the automated index configuration pipeline.
}
