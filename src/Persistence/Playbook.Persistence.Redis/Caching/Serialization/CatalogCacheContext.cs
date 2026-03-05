using System.Text.Json.Serialization;
using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis.Caching.Serialization;

// --- Catalog Module Context ---
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(CacheEnvelope<ProductDto>))] // Required for the wrapper!
internal partial class CatalogCacheContext : JsonSerializerContext { }