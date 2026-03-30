using System.Buffers;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

internal sealed class BufferWriterStream(IBufferWriter<byte> writer) : Stream
{
    public override void Write(byte[] buffer, int offset, int count) => writer.Write(buffer.AsSpan(offset, count));
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get; set; }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
