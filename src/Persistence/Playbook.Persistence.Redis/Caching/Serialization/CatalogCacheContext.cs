using System.Text.Json.Serialization;
using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis.Caching.Serialization;

// --- Catalog Module Context ---
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(CacheEnvelope<ProductDto>))]
[JsonSerializable(typeof(CacheEnvelope<List<ProductDto>>))]
internal partial class CatalogCacheContext : JsonSerializerContext;