using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise minimum over two input blocks: output[i] = Min(a[i], b[i]).
/// </summary>
public sealed class MinBlockMap(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryBlockMapBase(a, b, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        TensorPrimitives.Min(aBlock, bBlock, outputBlock);
    }
}


