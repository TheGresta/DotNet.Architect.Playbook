using Playbook.Persistence.Meilisearch.Core.Constants;
using Playbook.Persistence.Meilisearch.Core.Models;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Configuration;

/// <summary>
/// Concrete implementation of the <see cref="MeiliIndexConfiguration{T}"/> for the <see cref="CarDocument"/> model.
/// This class defines the specific index identity and domain-specific search behavior, 
/// such as linguistic synonyms for automotive terminology.
/// </summary>
/// <remarks>
/// By inheriting from the base configuration, this class automatically maps 
/// [MeiliFilterable] and [MeiliSortable] attributes from the <see cref="CarDocument"/> 
/// to the Meilisearch engine settings.
/// </remarks>
public sealed class CarDocumentIndexConfiguration : MeiliIndexConfiguration<CarDocument>
{
    /// <summary>
    /// Gets the unique identifier for the car documents index, 
    /// sourced from global system constants.
    /// </summary>
    public override string IndexName => MeiliConstants.IndexName;

    /// <summary>
    /// Defines a set of automotive-specific synonyms to improve search recall.
    /// Maps common abbreviations, regional terms, and frequent misspellings.
    /// </summary>
    /// <returns>A dictionary containing term mappings for the search engine.</returns>
    public override Dictionary<string, IEnumerable<string>> GetSynonyms() => new()
    {
        // Maps "hybrid" to common variations to ensure consistent search results.
        ["hybrid"] = ["hyrbrid", "plug in hybrid"],

        // Industry-standard abbreviations for electric vehicles.
        ["electric"] = ["ev", "bev"],

        // Regional terminology (e.g., US "gasoline" vs. UK "petrol" vs. European "benzin").
        ["petrol"] = ["gasoline", "benzin"]
    };
}
