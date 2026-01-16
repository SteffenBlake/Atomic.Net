using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes the bitwise AND of two input blocks and stores the result in this block.
/// </summary>
public sealed class AndMapBlock(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0,
    bool dense = false,
    ushort blockSize = 16
) : BinaryBlockMapBase(a, b, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        TensorPrimitives.BitwiseAnd(aBlock, bBlock, outputBlock);
    }
}


