using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Playbook.Persistence.Redis.Interfaces;

namespace Playbook.Persistence.Redis.Caching.Serialization;

public sealed class CompositeCacheSerializer : ICacheSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(
            IdentityCacheContext.Default,
            CatalogCacheContext.Default
        ),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public void Serialize<T>(IBufferWriter<byte> writer, T value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, value, Options);
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return default;

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}