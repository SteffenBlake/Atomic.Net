using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// BlockMap for exposing mutable Input Floats for Behaviors
/// </summary>
public sealed class InputBlockMap(
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BlockMapBase([], initValue, dense, blockSize)
{
    public void Set(int entityIndex, float value)
    {
        var (blockIndex, laneIndex) = GetBlockAndLane(entityIndex);
        var block = _outputs[blockIndex] ??= InitBlock();
        if (block[laneIndex] == value)
        {
            return;
        }

        block[laneIndex] = value;
        MakeDirty(blockIndex); 
    }

    /// <summary>
    /// Creates a BackedFloat pointing to a specific entity index in this block map.
    /// </summary>
    public BackedFloat InstanceFor(int entityIndex)
    {
        var (blockIndex, _) = GetBlockAndLane(entityIndex);
        _outputs[blockIndex] ??= InitBlock();
        return new BackedFloat(this, entityIndex);
    }

    protected override void RecomputeBlock(int blockIndex)
    {
    }
}
