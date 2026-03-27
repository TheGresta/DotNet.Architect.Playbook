using System.Text.Json.Serialization;

using Playbook.Persistence.Meilisearch.Core.Attributes;

namespace Playbook.Persistence.Meilisearch.Core.Models;

/// <summary>
/// .NET 10 High-Performance Car Document.
/// Optimized for Zero-Allocation JSON paths and Native AOT compatibility.
/// </summary>
public record CarDocument(
    [property: JsonPropertyName("id")] int Id,
    [property: MeiliFilterable]
    [property: JsonPropertyName("company")] string Company,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("engine")] string Engine,
    [property: JsonPropertyName("cc_capacity")] string CcCapacity,
    [property: MeiliSortable]
    [property: JsonPropertyName("horsepower")] int Horsepower,
    [property: MeiliSortable]
    [property: JsonPropertyName("top_speed_kmh")] int TopSpeedKmh,
    [property: JsonPropertyName("accel_0_100_sec")] double Accel0100Sec,
    [property: MeiliFilterable]
    [property: MeiliSortable]
    [property: JsonPropertyName("price_usd")] decimal PriceUsd,
    [property: MeiliFilterable]
    [property: JsonPropertyName("fuel_type")] string FuelType,
    [property: JsonPropertyName("seats")] int Seats,
    [property: JsonPropertyName("torque_nm")] int TorqueNm
);

