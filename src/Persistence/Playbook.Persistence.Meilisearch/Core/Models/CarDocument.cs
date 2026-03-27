using System.Text.Json.Serialization;

using Playbook.Persistence.Meilisearch.Core.Attributes;

namespace Playbook.Persistence.Meilisearch.Core.Models;

/// <summary>
/// A high-performance automotive domain model optimized for Meilisearch indexing and .NET 10 Native AOT 
/// (Ahead-Of-Time) compilation. This record utilizes source-generated JSON serialization to ensure 
/// zero-allocation paths and minimal memory footprint during high-frequency search operations.
/// </summary>
/// <param name="Id">The unique primary key for the document.</param>
/// <param name="Company">The automotive manufacturer. Marked as <see cref="MeiliFilterableAttribute"/> for faceted search.</param>
/// <param name="Model">The specific vehicle model designation.</param>
/// <param name="Engine">The powerplant configuration (e.g., V8, Electric Motor).</param>
/// <param name="CcCapacity">The engine displacement or battery capacity representation.</param>
/// <param name="Horsepower">The output power rating. Marked as <see cref="MeiliSortableAttribute"/> for performance ranking.</param>
/// <param name="TopSpeedKmh">The maximum rated velocity in km/h. Marked as <see cref="MeiliSortableAttribute"/>.</param>
/// <param name="Accel0100Sec">The acceleration metric from 0 to 100 km/h in seconds.</param>
/// <param name="PriceUsd">The market valuation in USD. Dual-indexed for both filtering and sorting.</param>
/// <param name="FuelType">The energy source. Marked as <see cref="MeiliFilterableAttribute"/> for category browsing.</param>
/// <param name="Seats">The passenger occupancy capacity.</param>
/// <param name="TorqueNm">The rotational force output in Newton-meters.</param>
public record CarDocument(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("company"), MeiliFilterable] string Company,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("engine")] string Engine,
    [property: JsonPropertyName("cc_capacity")] string CcCapacity,
    [property: JsonPropertyName("horsepower"), MeiliSortable] int Horsepower,
    [property: JsonPropertyName("top_speed_kmh"), MeiliSortable] int TopSpeedKmh,
    [property: JsonPropertyName("accel_0_100_sec")] double Accel0100Sec,
    [property: JsonPropertyName("price_usd"), MeiliFilterable, MeiliSortable] decimal PriceUsd,
    [property: JsonPropertyName("fuel_type"), MeiliFilterable] string FuelType,
    [property: JsonPropertyName("seats")] int Seats,
    [property: JsonPropertyName("torque_nm")] int TorqueNm
);
