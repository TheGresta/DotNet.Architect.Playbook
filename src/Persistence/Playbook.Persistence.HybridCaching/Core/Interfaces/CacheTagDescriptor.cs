namespace Playbook.Persistence.HybridCaching.Core.Interfaces;

/// <summary>
/// Represents a single, typed tag dimension used to classify a cache entry for grouped invalidation.
/// </summary>
/// <remarks>
/// A tag descriptor pairs a logical <see cref="Dimension"/> (e.g., "vendor", "region", "entity")
/// with a specific <see cref="Value"/> (e.g., "42", "eu-west", "product"). This model is consumed
/// by <see cref="ICacheTagFactory"/> to produce fully qualified, namespaced tag strings.
/// Use the static factory methods (<see cref="Vendor"/>, <see cref="Entity"/>, <see cref="Region"/>)
/// to create standard descriptors without coupling to raw string literals.
/// </remarks>
/// <param name="Dimension">The logical classification axis (e.g., "vendor", "region", "entity").</param>
/// <param name="Value">The specific identifier within that dimension (e.g., a vendor ID, region code).</param>
public sealed record CacheTagDescriptor(string Dimension, string Value)
{
    /// <summary>
    /// Creates a vendor-scoped tag descriptor.
    /// </summary>
    /// <param name="vendorId">The unique identifier of the vendor.</param>
    /// <returns>A <see cref="CacheTagDescriptor"/> with dimension "vendor".</returns>
    public static CacheTagDescriptor Vendor(string vendorId) => new("vendor", vendorId);

    /// <summary>
    /// Creates an entity-type-scoped tag descriptor, useful for invalidating all entries of a specific entity type.
    /// </summary>
    /// <param name="entityId">A logical name or identifier for the entity type (e.g., "product", "order").</param>
    /// <returns>A <see cref="CacheTagDescriptor"/> with dimension "entity".</returns>
    public static CacheTagDescriptor Entity(string entityId) => new("entity", entityId);

    /// <summary>
    /// Creates a region-scoped tag descriptor for geo-partitioned invalidation strategies.
    /// </summary>
    /// <param name="regionCode">The geographic or logical region identifier (e.g., "eu-west", "us-east").</param>
    /// <returns>A <see cref="CacheTagDescriptor"/> with dimension "region".</returns>
    public static CacheTagDescriptor Region(string regionCode) => new("region", regionCode);
}