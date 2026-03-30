using System.Buffers;
using System.IO.Compression;

using Microsoft.Extensions.Caching.Hybrid;

using ProtoBuf;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

public sealed class SmartTechSerializer<T> : IHybridCacheSerializer<T>
{
    private const byte _rawFlag = 0x00;
    private const byte _compressedFlag = 0x01;
    private const int _compressionThreshold = 51200; // 50KB

    public T Deserialize(ReadOnlySequence<byte> source)
    {
        if (source.IsEmpty) return default!;

        // 1. Read the 1-byte header flag
        var firstByte = source.FirstSpan[0];
        var data = source.Slice(1);

        // 2. Prepare the stream from the remaining sequence
        using var ms = new MemoryStream(data.ToArray());

        if (firstByte == _compressedFlag)
        {
            using var decompressionStream = new BrotliStream(ms, CompressionMode.Decompress);
            return Serializer.Deserialize<T>(decompressionStream);
        }

        return Serializer.Deserialize<T>(ms);
    }

    public void Serialize(T value, IBufferWriter<byte> target)
    {
        // 1. Serialize to a temporary buffer to check size
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, value);
        var bytes = ms.ToArray();

        if (bytes.Length > _compressionThreshold)
        {
            // 2a. Write Compressed Header
            var span = target.GetSpan(1);
            span[0] = _compressedFlag;
            target.Advance(1);

            // 2b. Compress directly into the target buffer writer
            using (var compressionStream = new BrotliStream(new BufferWriterStream(target), CompressionLevel.Fastest))
            {
                compressionStream.Write(bytes);
            }

            Console.WriteLine($"[Cache] High-Payload: {typeof(T).Name} compressed from {bytes.Length} bytes.");
        }
        else
        {
            // 3a. Write Raw Header
            var span = target.GetSpan(1);
            span[0] = _rawFlag;
            target.Advance(1);

            // 3b. Write raw bytes
            target.Write(bytes);
        }
    }
}
