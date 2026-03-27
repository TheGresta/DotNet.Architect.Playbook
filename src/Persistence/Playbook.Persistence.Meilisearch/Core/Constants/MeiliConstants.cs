using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Core.Constants;

/// <summary>
/// Centralizes global constants and schema-related metadata for the Meilisearch integration.
/// This class ensures that index names and field identifiers remain consistent across 
/// the entire persistence layer.
/// </summary>
public static class MeiliConstants
{
    /// <summary>
    /// The unique identifier for the primary search index containing automotive documents.
    /// </summary>
    public const string IndexName = "cars";

    /// <summary>
    /// Provides high-performance, type-safe field name resolution derived directly from the 
    /// <see cref="CarDocument"/> model. These fields are pre-computed using the 
    /// <see cref="MeiliFilterBuilder{T}"/> cache to avoid runtime reflection 
    /// during query construction.
    /// </summary>
    /// <remarks>
    /// Using this nested class ensures that if a <c>[JsonPropertyName]</c> is updated 
    /// in the model, the change propagates throughout the application without 
    /// manual string refactoring.
    /// </remarks>
    public static class Fields
    {
        /// <summary>The unique document identifier.</summary>
        public static readonly string Id = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.Id);

        /// <summary>The vehicle manufacturer name.</summary>
        public static readonly string Company = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.Company);

        /// <summary>The propulsion system type (e.g., Electric, Petrol).</summary>
        public static readonly string FuelType = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.FuelType);

        /// <summary>The vehicle valuation in US Dollars.</summary>
        public static readonly string PriceUsd = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.PriceUsd);

        /// <summary>The engine output power rating.</summary>
        public static readonly string Horsepower = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.Horsepower);

        /// <summary>The maximum rated velocity in kilometers per hour.</summary>
        public static readonly string TopSpeed = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.TopSpeedKmh);
    }
}
