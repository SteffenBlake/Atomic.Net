namespace Atomic.Net.MonoGame.Core.Sequencing;

public class ActionSequence(Action action) : Sequence
{
    private bool _hasRun = false;

    protected override double UpdateInternal(double elapsedSeconds)
    {
        if (_hasRun)
        {
            return elapsedSeconds;
        }

        action();
        _hasRun = true;

        return elapsedSeconds;
    }

    protected override void ResetInternal()
    {
        _hasRun = false;
    }
}


