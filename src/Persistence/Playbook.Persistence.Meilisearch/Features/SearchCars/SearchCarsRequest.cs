using System.ComponentModel.DataAnnotations;

namespace Playbook.Persistence.Meilisearch.Features.SearchCars;

/// <summary>
/// A high-performance Data Transfer Object (DTO) used to encapsulate inbound search parameters.
/// This record facilitates the mapping of API query strings or body parameters into the 
/// internal <see cref="MeiliSearchDescriptor{T}"/>, ensuring a clean separation between 
/// the transport layer and the search infrastructure.
/// </summary>
/// <param name="SearchTerm">The primary text-based query for keyword matching.</param>
/// <param name="Company">An optional filter for the automotive manufacturer.</param>
/// <param name="FuelType">An optional filter for the energy source (e.g., Electric, Hybrid).</param>
/// <param name="MinPrice">The lower-bound threshold for price-based range filtering.</param>
/// <param name="MaxPrice">The upper-bound threshold for price-based range filtering.</param>
/// <param name="Limit">The maximum number of results to return per page. Defaults to 20.</param>
/// <param name="Offset">The number of items to skip for pagination. Defaults to 0.</param>
public record SearchCarsRequest(
    string? SearchTerm = null,
    string? Company = null,
    string? FuelType = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    [Range(1, 100)] int Limit = 20,
    [Range(0, int.MaxValue)] int Offset = 0
);
