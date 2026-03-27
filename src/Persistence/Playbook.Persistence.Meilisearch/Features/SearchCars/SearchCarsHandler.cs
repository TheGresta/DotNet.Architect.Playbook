using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

namespace Playbook.Persistence.Meilisearch.Features.SearchCars;

public sealed class SearchCarsHandler(ICarDocumentRepository repository)
{
    public async Task<SearchResponse<CarDocument>> HandleAsync(SearchCarsRequest request, CancellationToken ct = default)
        => await repository.SearchAsync(request.SearchTerm, q => CarSearchSpecs.ApplyStandardSearch(q, request), ct);
}
