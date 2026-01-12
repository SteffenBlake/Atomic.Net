using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes the bitwise OR of two input blocks and stores the result in this block.
/// </summary>
public sealed class OrMapBlock(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0,
    bool dense = false,
    ushort blockSize = 16
) : BinaryBlockMapBase(a, b, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        TensorPrimitives.BitwiseOr(aBlock, bBlock, outputBlock);
    }
}
