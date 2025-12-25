namespace Atomic.Net.MonoGame.Core.Sequencing;

public class DelaySequence(double delaySeconds) : Sequence
{
    private double _elapsed = 0;

    protected override double UpdateInternal(double secondsElapsed)
    {
        if (_elapsed >= delaySeconds)
        {
            return secondsElapsed;
        }

        _elapsed += secondsElapsed;

        var leftover = Math.Max(0.0, _elapsed - delaySeconds);
        _elapsed -= leftover;

        return leftover;
    }

    protected override void ResetInternal()
    {
        _elapsed = 0;
    }
}


