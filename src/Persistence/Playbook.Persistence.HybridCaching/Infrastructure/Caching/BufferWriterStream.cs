using System.Buffers;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

/// <summary>
/// Provides a write-only <see cref="Stream"/> wrapper around an <see cref="IBufferWriter{Byte}"/>.
/// </summary>
/// <remarks>
/// This adapter class allows stream-based APIs (such as <see cref="System.IO.Compression.BrotliStream"/> or 
/// <see cref="ProtoBuf.Serializer"/>) to write directly into a buffer managed by an <see cref="IBufferWriter{Byte}"/>.
/// This minimizes intermediate allocations and leverages the memory management efficiency of the buffer writer.
/// </remarks>
internal sealed class BufferWriterStream(IBufferWriter<byte> writer) : Stream
{
    /// <summary>
    /// Writes a sequence of bytes to the underlying <see cref="IBufferWriter{Byte}"/>.
    /// </summary>
    /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    public override void Write(byte[] buffer, int offset, int count) => writer.Write(buffer.AsSpan(offset, count));

    /// <summary>
    /// Gets a value indicating whether the current stream supports reading.
    /// Always returns <see langword="false"/>.
    /// </summary>
    public override bool CanRead => false;

    /// <summary>
    /// Gets a value indicating whether the current stream supports seeking.
    /// Always returns <see langword="false"/>.
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// Gets a value indicating whether the current stream supports writing.
    /// Always returns <see langword="true"/>.
    /// </summary>
    public override bool CanWrite => true;

    /// <summary>
    /// Gets the length in bytes of the stream. Not supported by this implementation.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown because the underlying writer does not expose total length.</exception>
    public override long Length => throw new NotSupportedException();

    /// <summary>
    /// Gets or sets the position within the current stream.
    /// </summary>
    /// <remarks>
    /// The setter is provided for compatibility with stream-based compressors that track relative progress, 
    /// though it does not affect the underlying <see cref="IBufferWriter{Byte}"/>.
    /// </remarks>
    public override long Position { get; set; }

    /// <summary>
    /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
    /// Implementation is a no-op as <see cref="IBufferWriter{Byte}"/> manages its own flushing/advancement logic.
    /// </summary>
    public override void Flush() { }

    /// <summary>
    /// Reads a sequence of bytes from the current stream. Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown because this is a write-only adapter.</exception>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <summary>
    /// Sets the position within the current stream. Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown because the stream does not support seeking.</exception>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <summary>
    /// Sets the length of the current stream. Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown because the stream length cannot be predefined for this writer.</exception>
    public override void SetLength(long value) => throw new NotSupportedException();
}
