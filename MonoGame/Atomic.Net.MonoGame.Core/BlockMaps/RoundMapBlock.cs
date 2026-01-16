using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Rounds each element in the input block to the nearest integer and stores the result in this block.
/// </summary>
public sealed class RoundMapBlock(
    BlockMapBase input,
    float initValue = 0,
    bool dense = false,
    ushort blockSize = 16
) : UnaryBlockMapBase(input, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] inputBlock, float[] outputBlock)
    {
        TensorPrimitives.Round(inputBlock, outputBlock);
    }
}


