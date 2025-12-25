namespace Atomic.Net.MonoGame.Core.Sequencing;

public class Sequence
{
    protected readonly List<Sequence> _children = [];

    public void AddChild(Sequence child) => _children.Add(child);

    public void Update(double elapsedSeconds)
    {
        var remainder = UpdateInternal(elapsedSeconds);

        if (remainder <= 0) 
        {
            return;
        }

        foreach (var child in _children)
        {
            child.Update(remainder);
        }
    }

    /// <summary>
    /// Advances this node's internal state. Should update _state accordingly. Returns leftover delta.
    /// </summary>
    protected virtual double UpdateInternal(double elapsedSeconds)
    {
        return elapsedSeconds;
    }

    public void Reset()
    {
        ResetInternal();
        foreach(var child in _children)
        {
            child.Reset();
        }
    }

    protected virtual void ResetInternal() {}
}
