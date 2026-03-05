using System.Buffers;

namespace Playbook.Persistence.Redis.Interfaces;

public interface ICacheSerializer
{
    void Serialize<T>(IBufferWriter<byte> writer, T value);
    T? Deserialize<T>(ReadOnlySpan<byte> bytes);
}