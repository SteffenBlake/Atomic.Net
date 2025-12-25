namespace Atomic.Net.MonoGame.Core.Sequencing;

public readonly record struct Tween(
    double StartValue, double EndValue, double DurationSeconds
)
{
    public static Tween Out(double durationSeconds = 1.0) => new(1.0, 0.0, durationSeconds);
    public static Tween In(double durationSeconds = 1.0) => new(0.0, 1.0, durationSeconds);
}

public static class SequenceExtensions
{
    public static T WithChild<T>(this Sequence sequence, T child)
        where T: Sequence
    {
        sequence.AddChild(child);
        return child;
    }

    public static TweenSequence ThenTween(
        this Sequence sequence, 
        Tween tween,
        Action<double> apply
    )
    {
        var child = new TweenSequence(
            tween.StartValue, tween.EndValue, tween.DurationSeconds, apply
        );
        sequence.AddChild(child);
        return child;
    }

    public static TweenSequence ThenTween(
        this Sequence sequence, 
        double startValue, 
        double endValue, 
        double durationSeconds, 
        Action<double> apply
    )
    {
        var child = new TweenSequence(startValue, endValue, durationSeconds, apply);
        sequence.AddChild(child);
        return child;
    }

    public static TweenSequence ThenTween(
        this Sequence sequence, 
        double startValue, 
        double endValue, 
        double durationSeconds, 
        Action<double> apply, 
        TransformFunction easing
    )
    {
        var child = new TweenSequence(
            startValue, 
            endValue, 
            durationSeconds, 
            val => apply(easing(val))
        );
        sequence.AddChild(child);
        return child;
    }

    public static TweenSequence ThenTween(
        this Sequence sequence, 
        Tween tween,
        Action<double> apply, 
        TransformFunction easing
    )
    {
        var child = new TweenSequence(
            tween.StartValue, tween.EndValue, tween.DurationSeconds,
            val => apply(easing(val))
        );
        sequence.AddChild(child);
        return child;
    }

    public static DelaySequence ThenDelay(this Sequence sequence, TimeSpan delay)
    {
        var child = new DelaySequence(delay.TotalSeconds);
        sequence.AddChild(child);
        return child;
    }

    public static DelaySequence ThenDelay(this Sequence sequence, double delaySeconds)
    {
        var child = new DelaySequence(delaySeconds);
        sequence.AddChild(child);
        return child;
    }

    public static WhereSequence Where(this Sequence sequence, Func<bool> condition)
    {
        var child = new WhereSequence(condition);
        sequence.AddChild(child);
        return child;
    }

    public static ActionSequence Then(this Sequence sequence, Action action)
    {
        var child = new ActionSequence(action);
        sequence.AddChild(child);
        return child;
    }

    public static AsyncSequence ThenAsync(
        this Sequence sequence, 
        Func<CancellationToken, Task> action
    )
    {
        var child = new AsyncSequence(action);
        sequence.AddChild(child);
        return child;
    }
}


