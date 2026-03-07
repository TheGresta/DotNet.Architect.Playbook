using System.Text.Json.Serialization;

using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis.Caching.Serialization;

/// <summary>
/// Provides a source-generated <see cref="JsonSerializerContext"/> for types within the Catalog module.
/// </summary>
/// <remarks>
/// This context enables high-performance, reflection-free serialization for <see cref="ProductDto"/> 
/// and its associated collections and envelopes.
/// </remarks>
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(CacheEnvelope<ProductDto>))]
[JsonSerializable(typeof(CacheEnvelope<List<ProductDto>>))]
internal partial class CatalogCacheContext : JsonSerializerContext;
