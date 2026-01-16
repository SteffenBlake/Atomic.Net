namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Base class for scalar-output block maps that produce a single int value from one or more input blocks.
/// Tracks dirty state and triggers recalculation only when inputs have changed.
/// </summary>
public abstract class IntMapBase : MapBase
{
    private readonly int _blockCount;

    public int? Value { get; protected set; }

    private bool _dirty = true;

    protected IntMapBase(
        IEnumerable<BlockMapBase> dependencies,
        int? initValue = null
    )
    {
        _blockCount = dependencies.FirstOrDefault()?.BlockCount ?? 0;

        Value = initValue;

        foreach(var dependency in dependencies)
        {
            dependency.OnDirty += (_) => MakeDirty();
        }
    }

    /// <summary>
    /// Marks a block as dirty and cascades to all dependent nodes.
    /// </summary>
    /// <param name="blockIndex">The block index to mark dirty</param>
    protected void MakeDirty()
    {
        if (_dirty)
        {
            return;
        }

        _dirty = true;
        for(var blockIndex = 0; blockIndex < _blockCount; blockIndex++)
        {
            OnDirty?.Invoke(blockIndex);
        }
    }

    /// <summary>
    /// Triggers a recalculation of a given block index if it is dirty
    /// </summary>
    /// <param name="blockIndex">The block index</param>
    /// <returns>The computed Vector512 for this block</returns>
    public int? Recalculate()
    {
        if (!_dirty)
        {
            return Value;
        }

        Recompute();
        _dirty = false;

        return Value;
    }

    protected abstract void Recompute();
}
