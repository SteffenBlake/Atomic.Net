namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Base class for block maps that compute element-wise values from two input blocks.
/// </summary>
public abstract class BinaryBlockMapBase(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BlockMapBase([a, b], initValue, dense, blockSize)
{
    protected override void RecomputeBlock(int blockIndex)
    {
        a.RecalculateBlock(blockIndex);
        b.RecalculateBlock(blockIndex);

        if (!a.TryGetBlock(blockIndex, out var aBlock))
        {
            return;
        }
        if (!b.TryGetBlock(blockIndex, out var bBlock))
        {
            return;
        }

        var outputBlock = _outputs[blockIndex] ??= InitBlock();

        SIMDBlock(aBlock, bBlock, outputBlock);
    }

    protected abstract void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock);
}


