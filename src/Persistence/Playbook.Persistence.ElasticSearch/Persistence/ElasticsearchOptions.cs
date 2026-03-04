using System.ComponentModel.DataAnnotations;

namespace Playbook.Persistence.ElasticSearch.Persistence;

public record ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    [Required, Url]
    public required string Url { get; init; }

    [Required]
    public required string DefaultIndex { get; init; }

    // Optional: For Basic Authentication
    public string? Username { get; init; }
    public string? Password { get; init; }

    // Optional: For Cloud/Production API Key Auth
    public string? ApiKey { get; init; }

    // Resilience Settings
    public int MaxRetries { get; init; } = 3;
    public bool SniffOnStartup { get; init; } = false;
    public bool EnableDebugMode { get; init; } = false;
}