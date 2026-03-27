using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Core.Constants;

public static class MeiliConstants
{
    public const string IndexName = "cars";

    /// <summary>
    /// Type-safe field names derived directly from the CarDocument record.
    /// If you change the JsonPropertyName in the record, these update automatically.
    /// </summary>
    public static class Fields
    {
        public static readonly string Id = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.Id);
        public static readonly string Company = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.Company);
        public static readonly string FuelType = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.FuelType);
        public static readonly string PriceUsd = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.PriceUsd);
        public static readonly string Horsepower = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.Horsepower);
        public static readonly string TopSpeed = MeiliFilterBuilder<CarDocument>.GetCachedPropertyName(x => x.TopSpeedKmh);
    }
}
