namespace Playbook.Persistence.Redis.Caching.Serialization;

/// <summary>
/// A wrapper to store metadata alongside the cached value (e.g., creation date).
/// </summary>
internal readonly record struct CacheEnvelope<T>(
    T Value,
    DateTimeOffset CreatedAt,
    long Version = 1);
