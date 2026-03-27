using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Features.SearchCars;

public static class CarSearchSpecs
{
    public static void ApplyStandardSearch(MeiliSearchDescriptor<CarDocument> q, SearchCarsRequest req) => q.Paging(req.Limit, req.Offset)
         .WithFilters(f => f
            .WhereEquals(x => x.Company, req.Company)
            .WhereGreaterThanOrEqual(x => x.PriceUsd, req.MinPrice))
         .SortByDescending(x => x.PriceUsd)
         .Highlight(x => x.Model);
}
