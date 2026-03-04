using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Playbook.Persistence.Redis.Application;

namespace Playbook.Persistence.Redis.Caching.Serialization;

public sealed class CompositeCacheSerializer : ICacheSerializer
{
    private readonly JsonSerializerOptions _options;

    public CompositeCacheSerializer()
    {
        _options = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                IdentityCacheContext.Default,
                CatalogCacheContext.Default,
                // Add new module contexts here as the system grows
                new DefaultJsonTypeInfoResolver() // Fallback for primitive types
            ),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, _options);
    }

    public T? Deserialize<T>(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(bytes, _options);
    }
}