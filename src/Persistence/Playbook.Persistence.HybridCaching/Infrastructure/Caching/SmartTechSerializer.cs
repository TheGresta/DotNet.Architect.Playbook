using System.Buffers;
using System.IO.Compression;

using Microsoft.Extensions.Caching.Hybrid;

using ProtoBuf;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

/// <summary>
/// Provides a high-performance, hybrid serialization mechanism for <typeparamref name="T"/> 
/// that integrates Brotli compression for large payloads.
/// </summary>
/// <remarks>
/// This implementation evaluates the serialized payload size against a defined threshold.
/// Small payloads are stored as raw bytes, while large payloads (>= 50KB) are compressed 
/// to optimize storage footprint and network I/O in distributed caching scenarios.
/// </remarks>
/// <typeparam name="T">The type of the object to be serialized or deserialized.</typeparam>
public sealed class SmartTechSerializer<T> : IHybridCacheSerializer<T>
{
    /// <summary>
    /// Indicates that the payload is stored in its raw, uncompressed format.
    /// </summary>
    private const byte _rawFlag = 0x00;

    /// <summary>
    /// Indicates that the payload has been compressed using the Brotli algorithm.
    /// </summary>
    private const byte _compressedFlag = 0x01;

    /// <summary>
    /// The threshold (in bytes) at which compression is applied to the payload.
    /// Current setting: 50KB.
    /// </summary>
    private const int _compressionThreshold = 51200; // 50KB

    /// <summary>
    /// Deserializes the provided <see cref="ReadOnlySequence{Byte}"/> back into an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="source">The byte sequence containing the serialized data and the 1-byte control header.</param>
    /// <returns>An instance of <typeparamref name="T"/>; returns <see langword="default"/> if the source is empty.</returns>
    /// <exception cref="InvalidDataException">Thrown if the compression header is unrecognized or the stream is corrupted.</exception>
    public T Deserialize(ReadOnlySequence<byte> source)
    {
        if (source.IsEmpty) return default!;

        // Extract the 1-byte header to determine if the subsequent payload is compressed.
        var firstByte = source.FirstSpan[0];
        var data = source.Slice(1);

        // Convert sequence to a stream-compatible format for the underlying ProtoBuf/System.Text.Json serializer.
        using var ms = new MemoryStream(data.ToArray());

        if (firstByte == _compressedFlag)
        {
            // Wrap the memory stream in a Brotli decompression layer if the compression flag is set.
            using var decompressionStream = new BrotliStream(ms, CompressionMode.Decompress);
            return Serializer.Deserialize<T>(decompressionStream);
        }

        return Serializer.Deserialize<T>(ms);
    }

    /// <summary>
    /// Serializes the specified value into the provided <see cref="IBufferWriter{Byte}"/>.
    /// </summary>
    /// <param name="value">The object instance to serialize.</param>
    /// <param name="target">The buffer writer to which the serialized data and header are written.</param>
    public void Serialize(T value, IBufferWriter<byte> target)
    {
        // Perform an initial serialization to an intermediate buffer to evaluate size against the threshold.
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, value);
        var bytes = ms.ToArray();

        if (bytes.Length > _compressionThreshold)
        {
            // Prepend the header flag indicating a compressed payload.
            var span = target.GetSpan(1);
            span[0] = _compressedFlag;
            target.Advance(1);

            // Utilize BrotliStream with Fastest compression to balance CPU overhead and storage efficiency.
            // BufferWriterStream acts as a bridge between the Stream-based compressor and the IBufferWriter.
            using (var compressionStream = new BrotliStream(new BufferWriterStream(target), CompressionLevel.Fastest))
            {
                compressionStream.Write(bytes);
            }

            Console.WriteLine($"[Cache] High-Payload: {typeof(T).Name} compressed from {bytes.Length} bytes.");
        }
        else
        {
            // Prepend the header flag indicating a raw (uncompressed) payload.
            var span = target.GetSpan(1);
            span[0] = _rawFlag;
            target.Advance(1);

            // Copy the raw bytes directly to the target buffer writer for low-latency processing.
            target.Write(bytes);
        }
    }
}
