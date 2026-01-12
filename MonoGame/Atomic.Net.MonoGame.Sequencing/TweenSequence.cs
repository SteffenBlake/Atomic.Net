namespace Atomic.Net.MonoGame.Sequencing;

public class TweenSequence(
    double startValue, 
    double endValue, 
    double durationSeconds, 
    Action<double> apply
) : Sequence
{
    private double _elapsed;
    private readonly double _valueDelta = endValue - startValue;

    protected override double UpdateInternal(double elapsedSeconds)
    {
        if (_elapsed >= durationSeconds)
        {
            return elapsedSeconds;
        }

        _elapsed += elapsedSeconds;

        var leftover = Math.Max(0f, _elapsed - durationSeconds);
        _elapsed -= leftover;

        var progress = _elapsed / durationSeconds;
        var currentValue = startValue + _valueDelta * progress;
        apply(currentValue);

        return leftover;
    }

    protected override void ResetInternal()
    {
        _elapsed = 0;
    }
}


