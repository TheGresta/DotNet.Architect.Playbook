using System.Buffers;

namespace Playbook.Persistence.Redis.Interfaces;

/// <summary>
/// Defines the contract for cache serialization providers used in multi-level caching systems.
/// </summary>
public interface ICacheSerializer
{
    /// <summary>
    /// Serializes an object of type <typeparamref name="T"/> to the provided buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="writer">The buffer writer to receive the serialized data.</param>
    /// <param name="value">The value to serialize.</param>
    void Serialize<T>(IBufferWriter<byte> writer, T value);

    /// <summary>
    /// Deserializes the provided byte span into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="bytes">The source bytes to deserialize.</param>
    /// <returns>The deserialized object, or <see langword="default"/> if the operation cannot be completed.</returns>
    T? Deserialize<T>(ReadOnlySpan<byte> bytes);
}
