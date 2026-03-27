namespace Playbook.Persistence.Meilisearch.Features.SearchCars;

public record SearchCarsRequest(
    string? SearchTerm = null,
    string? Company = null,
    string? FuelType = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Limit = 20,
    int Offset = 0
);
