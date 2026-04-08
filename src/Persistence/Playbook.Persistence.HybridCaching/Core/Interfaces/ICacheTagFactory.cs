namespace Playbook.Persistence.HybridCaching.Core.Interfaces;

/// <summary>
/// Defines a contract for building fully qualified, namespaced cache tag strings
/// from a set of <see cref="CacheTagDescriptor"/> instances.
/// </summary>
/// <remarks>
/// The tag factory centralizes the tag formatting convention and supports multi-dimensional
/// invalidation strategies. Each <see cref="CacheTagDescriptor"/> contributes one tag string
/// in the format: <c>tag:{SchemaVersion}:{Namespace}:{prefix}:{dimension}:{value}</c>.
/// <para>
/// This decouples tag string construction from policy definitions and from
/// <see cref="ICacheKeyProvider"/>, enabling policies to express rich, composable
/// invalidation scopes (e.g., vendor + region + entity) without modifying any infrastructure code.
/// </para>
/// </remarks>
public interface ICacheTagFactory
{
    /// <summary>
    /// Builds an array of fully qualified cache tag strings from the provided descriptors.
    /// </summary>
    /// <param name="prefix">The logical category or domain prefix shared by all tags (e.g., "product", "order").</param>
    /// <param name="descriptors">
    /// The collection of <see cref="CacheTagDescriptor"/> instances that define the tag dimensions and values.
    /// Returns an empty array if <paramref name="descriptors"/> is null or contains no elements.
    /// </param>
    /// <returns>
    /// An array of tag strings. Each element follows the pattern:
    /// <c>tag:{SchemaVersion}:{Namespace}:{prefix}:{dimension}:{value}</c>.
    /// </returns>
    string[] Build(string prefix, IEnumerable<CacheTagDescriptor>? descriptors);
}