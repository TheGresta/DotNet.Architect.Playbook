namespace Playbook.Persistence.Meilisearch.Core.Models;

public static class MeiliConstants
{
    public const string IndexName = "cars";

    /// <summary>
    /// Explicitly defined field names matching the JSON schema.
    /// Used for indexing configuration and internal caching.
    /// </summary>
    public static class Fields
    {
        public const string Id = "id";
        public const string Company = "company";
        public const string Model = "model";
        public const string Price = "price_usd";
        public const string FuelType = "fuel_type";
        public const string Horsepower = "horsepower";
        public const string CcCapacity = "cc_capacity";
        public const string TopSpeed = "top_speed_kmh";
        public const string Accel = "accel_0_100_sec";
        public const string Torque = "torque_nm";
    }

    public static readonly string[] FilterableAttributes =
    [
        Fields.Company,
        Fields.FuelType,
        Fields.Price
    ];

    public static readonly string[] SortableAttributes =
    [
        Fields.Price,
        Fields.Horsepower,
        Fields.TopSpeed
    ];
}
