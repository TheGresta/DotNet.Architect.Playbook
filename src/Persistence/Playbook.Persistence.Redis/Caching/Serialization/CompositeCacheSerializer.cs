using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Playbook.Persistence.Redis.Interfaces;

namespace Playbook.Persistence.Redis.Caching.Serialization;

/// <summary>
/// Implements a high-performance JSON serializer that aggregates multiple <see cref="JsonSerializerContext"/> 
/// definitions to support hybrid L1/L2 caching scenarios.
/// </summary>
/// <remarks>
/// This implementation uses <see cref="JsonTypeInfoResolver.Combine"/> to merge metadata from 
/// <see cref="IdentityCacheContext"/> and <see cref="CatalogCacheContext"/>, ensuring compatibility 
/// with AOT (Ahead-of-Time) compilation and reducing reflection overhead.
/// </remarks>
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

    /// <summary>
    /// Serializes the specified value into a <see cref="IBufferWriter{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="writer">The <see cref="IBufferWriter{Byte}"/> to which the JSON payload is written.</param>
    /// <param name="value">The object to be serialized into the buffer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method utilizes <see cref="Utf8JsonWriter"/> for zero-allocation writing where possible, 
    /// making it suitable for high-throughput Redis or L2 cache operations.
    /// </remarks>
    public void Serialize<T>(IBufferWriter<byte> writer, T value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, value, Options);
    }

    /// <summary>
    /// Deserializes a <see cref="ReadOnlySpan{Byte}"/> into an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type for deserialization.</typeparam>
    /// <param name="bytes">The UTF-8 encoded byte span containing the JSON data.</param>
    /// <returns>
    /// An instance of <typeparamref name="T"/> if deserialization succeeds; otherwise, <see langword="default"/>.
    /// </returns>
    /// <remarks>
    /// If the input <paramref name="bytes"/> is empty or a <see cref="JsonException"/> occurs during processing, 
    /// the method returns <see langword="default"/> rather than propagating the exception, ensuring 
    /// cache-miss behavior rather than application failure.
    /// </remarks>
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
