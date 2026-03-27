using MeiliIndex = Meilisearch.Index;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Configuration;

/// <summary>
/// Defines the contract for index-specific settings and synchronization logic.
/// </summary>
public interface IMeiliIndexConfiguration
{
    string IndexName { get; }
    Task ConfigureAsync(MeiliIndex index, CancellationToken ct);
}
