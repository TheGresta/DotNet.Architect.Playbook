using System.Text.Json.Serialization;

namespace Playbook.Persistence.Meilisearch.Core.Models;

/// <summary>
/// .NET 10 High-Performance Car Document.
/// Optimized for Zero-Allocation JSON paths and Native AOT compatibility.
/// </summary>
public record CarDocument(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("company")] string Company,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("engine")] string Engine,
    [property: JsonPropertyName("cc_capacity")] string CcCapacity,
    [property: JsonPropertyName("horsepower")] int Horsepower,
    [property: JsonPropertyName("top_speed_kmh")] int TopSpeedKmh,
    [property: JsonPropertyName("accel_0_100_sec")] double Accel0100Sec,
    [property: JsonPropertyName("price_usd")] decimal PriceUsd,
    [property: JsonPropertyName("fuel_type")] string FuelType,
    [property: JsonPropertyName("seats")] int Seats,
    [property: JsonPropertyName("torque_nm")] int TorqueNm
);
