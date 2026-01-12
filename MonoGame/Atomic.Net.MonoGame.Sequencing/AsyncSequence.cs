namespace Atomic.Net.MonoGame.Sequencing;

public class AsyncSequence(Func<CancellationToken, Task> action) : Sequence
{
    private Task? _task;
    private CancellationTokenSource _cts = new();

    protected override double UpdateInternal(double elapsedSeconds)
    {
        _task ??= action(_cts.Token);

        return _task.IsCompleted ? elapsedSeconds : 0;
    }

    protected override void ResetInternal()
    {
        _cts.Cancel();
        _cts = new();
        _task = null;
    }
}


