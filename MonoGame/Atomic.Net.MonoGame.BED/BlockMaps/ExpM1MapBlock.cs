using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes e^x - 1 for each element in the input block and stores the result in this block.
/// </summary>
public sealed class ExpM1MapBlock(
    BlockMapBase input,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : UnaryBlockMapBase(input, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] inputBlock, float[] outputBlock)
    {
        TensorPrimitives.ExpM1(inputBlock, outputBlock);
    }
}


