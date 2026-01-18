using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

public abstract class MapBase
{
    public Action<int>? OnDirty;
}

public abstract class BlockMapBase : MapBase
{
    public int BlockCount { get; }

    protected readonly int _blockSize;

    protected readonly float[]?[] _outputs;

    private readonly float _initValue;
    private readonly int[] _dirty;
    
    protected BlockMapBase(
        IEnumerable<MapBase> dependencies,
        float initValue = 0.0f,
        bool dense = false,
        ushort blockSize = 16
    )
    {
        _blockSize = blockSize;
        _initValue = initValue;

        BlockCount = Math.DivRem(Constants.MaxEntities, _blockSize, out int rem);
        if (rem != 0)
        {
            BlockCount++;
        }

        _dirty = [.. Enumerable.Repeat(1, BlockCount) ];
        _outputs = new float[]?[BlockCount];

        if (dense)
        {
            for (var blockIndex = 0; blockIndex < BlockCount; blockIndex++)
            {
                _outputs[blockIndex] = InitBlock();
            }
        }

        foreach(var dependency in dependencies)
        {
            dependency.OnDirty += MakeDirty;
        }
    }

    /// <summary>
    /// Initializes a new block with the value of <see cref="_initValue"/> 
    /// </summary>
    protected float[] InitBlock()
    {
        return [.. Enumerable.Repeat(_initValue, _blockSize) ];
    }

    /// <summary>
    /// Marks a block as dirty and cascades to all dependent nodes.
    /// </summary>
    /// <param name="blockIndex">The block index to mark dirty</param>
    protected void MakeDirty(int blockIndex)
    {
        if (_dirty[blockIndex] != 0)
        {
            return;
        }

        _dirty[blockIndex] = 1;
        OnDirty?.Invoke(blockIndex);
    }

    /// <summary>
    /// Helper to compute block and lane index for an entity index.
    /// </summary>
    protected (int BlockIndex, int LaneIndex) GetBlockAndLane(int entityIndex)
    {
        int block = Math.DivRem(entityIndex, _blockSize, out int lane);
        return (block, lane);
    }

    /// <summary>
    /// Read-only access to per-entity values. Returns null if the block hasn't been allocated.
    /// </summary>
    public float? this[int entityIndex]
    {
        get
        {
            var (blockIndex, laneIndex) = GetBlockAndLane(entityIndex);
            return _outputs[blockIndex]?[laneIndex];
        }
    }

    public bool TryGetBlock(
        int blockIndex, 
        [NotNullWhen(true)]
        out float[]? block
    )
    {
        block = _outputs[blockIndex];
        return block != null;
    }

    /// <summary>
    /// Recalculates all dirty blocks.
    /// Derived classes only provide ComputeBlock for the raw computation.
    /// </summary>
    public void Recalculate()
    {
        for (int blockIndex = 0; blockIndex < _outputs.Length; blockIndex++)
        {
            _ = RecalculateBlock(blockIndex);
        }
    }

    /// <summary>
    /// Triggers a recalculation of a given block index if it is dirty
    /// </summary>
    /// <param name="blockIndex">The block index</param>
    /// <returns>The computed Vector512 for this block</returns>
    public float[]? RecalculateBlock(int blockIndex)
    {
        if (_dirty[blockIndex] == 0)
        {
            return _outputs[blockIndex];
        }

        RecomputeBlock(blockIndex);
        _dirty[blockIndex] = 0;

        return _outputs[blockIndex];
    }

    protected abstract void RecomputeBlock(int blockIndex);

    /// <summary>
    /// Creates a BackedFloat pointing to a specific entity index in this block map.
    /// </summary>
    public ReadOnlyBackedFloat ReadOnlyInstanceFor(int entityIndex)
    {
        var (blockIndex, _) = GetBlockAndLane(entityIndex);
        _outputs[blockIndex] ??= InitBlock();
        return new ReadOnlyBackedFloat(this, entityIndex);
    }
}


