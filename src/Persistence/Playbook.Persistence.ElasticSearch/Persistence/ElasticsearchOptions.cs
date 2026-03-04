using System.ComponentModel.DataAnnotations;

namespace Playbook.Persistence.ElasticSearch.Persistence;

/// <summary>
/// Represents the configuration settings for connecting to and interacting with Elasticsearch.
/// </summary>
/// <remarks>
/// These options are typically bound from the <c>"Elasticsearch"</c> section of the application configuration.
/// The class uses Data Annotations to enforce valid configuration at startup.
/// </remarks>
public record ElasticsearchOptions
{
    /// <summary>
    /// The default section name in configuration files.
    /// </summary>
    public const string SectionName = "Elasticsearch";

    /// <summary>
    /// Gets the URI of the Elasticsearch node or cluster.
    /// </summary>
    /// <value>A valid URL string (e.g., "http://localhost:9200" or an Elastic Cloud endpoint).</value>
    [Required, Url]
    public required string Url { get; init; }

    /// <summary>
    /// Gets the default index name to use when a specific index is not provided in a request.
    /// </summary>
    [Required]
    public required string DefaultIndex { get; init; }

    /// <summary>
    /// Gets the username for Basic Authentication.
    /// </summary>
    /// <remarks>
    /// Only used if <see cref="ApiKey"/> is not provided.
    /// </remarks>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the password for Basic Authentication.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets the API Key used for authentication, primarily in Elastic Cloud or secured production clusters.
    /// </summary>
    /// <remarks>
    /// If provided, this usually takes precedence over <see cref="Username"/> and <see cref="Password"/>.
    /// </remarks>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Gets the maximum number of times the client should retry a failed request.
    /// </summary>
    /// <value>Defaults to 3.</value>
    [Range(0, int.MaxValue)]
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets a value indicating whether the client should "sniff" the cluster state on startup to discover nodes.
    /// </summary>
    /// <remarks>
    /// Enabling this can improve resilience in multi-node clusters but should generally be <c>false</c> when using Load Balancers or Cloud environments.
    /// </remarks>
    public bool SniffOnStartup { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to enable detailed diagnostic logging and pretty-printed JSON.
    /// </summary>
    /// <remarks>
    /// <para>WARNING: Enabling this in production can lead to significant performance overhead and large log volumes.</para>
    /// </remarks>
    public bool EnableDebugMode { get; init; } = false;
}