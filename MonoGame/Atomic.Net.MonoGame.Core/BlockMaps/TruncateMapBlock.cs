using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Truncates each element in the input block (rounds toward zero) and stores the result in this block.
/// </summary>
public sealed class TruncateMapBlock(
    BlockMapBase input,
    float initValue = 0,
    bool dense = false,
    ushort blockSize = 16
) : UnaryBlockMapBase(input, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] inputBlock, float[] outputBlock)
    {
        TensorPrimitives.Truncate(inputBlock, outputBlock);
    }
}


