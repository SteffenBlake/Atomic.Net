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

    protected override void RecomputeBlock(int blockIndex)
    {
    }
}
