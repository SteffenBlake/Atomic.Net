namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Base class for block maps that compute element-wise values from three input blocks.
/// </summary>
public abstract class TernaryBlockMapBase(
    BlockMapBase a,
    BlockMapBase b,
    BlockMapBase c,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BlockMapBase([a, b, c], initValue, dense, blockSize)
{
    protected override void RecomputeBlock(int blockIndex)
    {
        a.RecalculateBlock(blockIndex);
        b.RecalculateBlock(blockIndex);
        c.RecalculateBlock(blockIndex);

        if (!a.TryGetBlock(blockIndex, out var aBlock))
        {
            return;
        }
        if (!b.TryGetBlock(blockIndex, out var bBlock))
        {
            return;
        }
        if (!c.TryGetBlock(blockIndex, out var cBlock))
        {
            return;
        }

        var outputBlock = _outputs[blockIndex] ??= InitBlock();

        SIMDBlock(aBlock, bBlock, cBlock, outputBlock);
    }

    protected abstract void SIMDBlock(
        float[] aBlock, float[] bBlock, float[] cBlock, float[] outputBlock
    );
}


