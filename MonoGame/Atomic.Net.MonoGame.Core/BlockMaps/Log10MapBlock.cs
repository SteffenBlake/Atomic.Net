using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes the base-10 logarithm of each element in the input block and stores the result in this block.
/// </summary>
public sealed class Log10MapBlock(
    BlockMapBase input,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : UnaryBlockMapBase(input, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] inputBlock, float[] outputBlock)
    {
        TensorPrimitives.Log10(inputBlock, outputBlock);
    }
}


