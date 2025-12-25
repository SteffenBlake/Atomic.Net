namespace Atomic.Net.MonoGame.Core.Sequencing;

public class WhereSequence(Func<bool> condition) : Sequence
{
    protected override double UpdateInternal(double elapsedSeconds)
    {
        if (!condition())
        {
            return 0.0;
        }

        return elapsedSeconds;
    }
}


