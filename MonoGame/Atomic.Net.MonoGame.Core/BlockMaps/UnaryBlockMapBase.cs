namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Base class for block maps that compute element-wise values from a single input block.
/// </summary>
public abstract class UnaryBlockMapBase(
    BlockMapBase input,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BlockMapBase([input], initValue, dense, blockSize)
{
    protected override void RecomputeBlock(int blockIndex)
    {
        input.RecalculateBlock(blockIndex);

        if (!input.TryGetBlock(blockIndex, out var inputBlock))
        {
            return;
        }

        var outputBlock = _outputs[blockIndex] ??= InitBlock();

        SIMDBlock(inputBlock, outputBlock);
    }

    protected abstract void SIMDBlock(float[] inputBlock, float[] outputBlock);
}


