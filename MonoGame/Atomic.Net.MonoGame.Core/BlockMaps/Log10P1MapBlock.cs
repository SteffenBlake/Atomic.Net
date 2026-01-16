using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes log10(1 + x) for each element in the input block and stores the result in this block.
/// </summary>
public sealed class Log10P1MapBlock(
    BlockMapBase input,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : UnaryBlockMapBase(input, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] inputBlock, float[] outputBlock)
    {
        TensorPrimitives.Log10P1(inputBlock, outputBlock);
    }
}


