namespace Playbook.Persistence.Redis.Caching.Serialization;

/// <summary>
/// Represents a metadata wrapper that encapsulates a cached value along with its temporal and versioning information.
/// </summary>
/// <typeparam name="T">The type of the value being cached.</typeparam>
/// <param name="Value">The underlying data being stored in the cache.</param>
/// <param name="CreatedAt">The <see cref="DateTimeOffset"/> indicating when the entry was originally created.</param>
/// <param name="Version">The schema or data version of the cached entry, defaulting to 1.</param>
internal readonly record struct CacheEnvelope<T>(
    T Value,
    DateTimeOffset CreatedAt,
    long Version = 1);
