using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes the reciprocal square root (1/sqrt(x)) of each element in the input block and stores the result in this block.
/// </summary>
public sealed class ReciprocalSqrtMapBlock(
    BlockMapBase input,
    float initValue = 0,
    bool dense = false,
    ushort blockSize = 16
) : UnaryBlockMapBase(input, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] inputBlock, float[] outputBlock)
    {
        TensorPrimitives.ReciprocalSqrt(inputBlock, outputBlock);
    }
}


