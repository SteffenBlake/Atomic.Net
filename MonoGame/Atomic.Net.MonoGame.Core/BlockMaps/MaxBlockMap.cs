using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise maximum over two input blocks: output[i] = Max(a[i], b[i]).
/// </summary>
public sealed class MaxBlockMap(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryBlockMapBase(a, b, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        TensorPrimitives.Max(aBlock, bBlock, outputBlock);
    }
}


