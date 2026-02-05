namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Wrapper stream that reports read progress via IProgress.
/// Used for tracking file read progress during async scene loading.
/// </summary>
public sealed class ProgressStream : Stream
{
    private readonly Stream _baseStream;
    private readonly IProgress<long> _progress;
    private long _bytesRead = 0;

    public ProgressStream(Stream baseStream, IProgress<long> progress)
    {
        _baseStream = baseStream;
        _progress = progress;
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _baseStream.Length;

    public override long Position
    {
        get => _baseStream.Position;
        set => throw new NotSupportedException("ProgressStream does not support seeking");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _baseStream.Read(buffer, offset, count);
        _bytesRead += bytesRead;
        _progress.Report(_bytesRead);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        _bytesRead += bytesRead;
        _progress.Report(_bytesRead);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _baseStream.ReadAsync(buffer, cancellationToken);
        _bytesRead += bytesRead;
        _progress.Report(_bytesRead);
        return bytesRead;
    }

    public override void Flush() => _baseStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("ProgressStream does not support seeking");
    public override void SetLength(long value) => throw new NotSupportedException("ProgressStream does not support SetLength");
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("ProgressStream does not support writing");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _baseStream.DisposeAsync();
        await base.DisposeAsync();
    }
}
