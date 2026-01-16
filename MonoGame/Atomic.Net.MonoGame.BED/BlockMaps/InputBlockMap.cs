using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

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
    /// Creates a BackedProperty pointing to a specific entity index in this block map.
    /// </summary>
    public BackedProperty<float> InstanceFor(int entityIndex)
    {
        var (blockIndex, laneIndex) = GetBlockAndLane(entityIndex);
        var block = _outputs[blockIndex] ??= InitBlock();
        return new BackedProperty<float>(block, laneIndex);
    }

    protected override void RecomputeBlock(int blockIndex)
    {
    }
}
