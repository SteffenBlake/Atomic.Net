namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Progress event fired during stream read operations.
/// Contains timestamp, bytes read in this operation, and total bytes read so far.
/// </summary>
public readonly record struct StreamProgressEvent(
    DateTime Timestamp,
    int BytesRead,
    long TotalBytesRead
);

/// <summary>
/// Stream wrapper that tracks read progress and reports it via IProgress.
/// Used to determine if JsonSerializer.DeserializeAsync reads incrementally or buffers up front.
/// </summary>
public sealed class ProgressTrackingStream : Stream
{
    private readonly Stream _innerStream;
    private readonly IProgress<StreamProgressEvent>? _progress;
    private long _totalBytesRead;

    public ProgressTrackingStream(Stream innerStream, IProgress<StreamProgressEvent>? progress = null)
    {
        _innerStream = innerStream;
        _progress = progress;
        _totalBytesRead = 0;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        _totalBytesRead += bytesRead;

        _progress?.Report(new StreamProgressEvent(
            DateTime.UtcNow,
            bytesRead,
            _totalBytesRead
        ));

        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        _totalBytesRead += bytesRead;

        _progress?.Report(new StreamProgressEvent(
            DateTime.UtcNow,
            bytesRead,
            _totalBytesRead
        ));

        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);
        _totalBytesRead += bytesRead;

        _progress?.Report(new StreamProgressEvent(
            DateTime.UtcNow,
            bytesRead,
            _totalBytesRead
        ));

        return bytesRead;
    }

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
