using Playbook.Persistence.Meilisearch.Core.Constants;
using Playbook.Persistence.Meilisearch.Core.Models;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Configuration;

public sealed class CarDocumentIndexConfiguration : MeiliIndexConfiguration<CarDocument>
{
    public override string IndexName => MeiliConstants.IndexName;

    public override Dictionary<string, IEnumerable<string>> GetSynonyms() => new()
    {
        ["hybrid"] = ["hyrbrid", "plug in hybrid"],
        ["electric"] = ["ev", "bev"],
        ["petrol"] = ["gasoline", "benzin"]
    };
}
