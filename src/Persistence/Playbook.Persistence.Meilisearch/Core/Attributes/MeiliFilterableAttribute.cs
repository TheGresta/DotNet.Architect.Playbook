namespace Playbook.Persistence.Meilisearch.Core.Attributes;

/// <summary>
/// A decorative metadata attribute used to mark a model property as "Filterable" 
/// within the Meilisearch engine. 
/// </summary>
/// <remarks>
/// When applied to a property in a document model, the <see cref="MeiliIndexConfiguration{T}"/> 
/// will automatically detect this attribute during the synchronization phase and update the 
/// Meilisearch index settings to allow filtering, faceted search, and bucketed results 
/// based on this property.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class MeiliFilterableAttribute : Attribute
{
    // This attribute acts as a marker for the reflection-based configuration discovery system.
}
