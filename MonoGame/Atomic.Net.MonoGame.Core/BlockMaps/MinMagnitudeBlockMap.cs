using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise minimum magnitude over two input blocks: output[i] = MinMagnitude(a[i], b[i]).
/// </summary>
public sealed class MinMagnitudeBlockMap(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryBlockMapBase(a, b, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        TensorPrimitives.MinMagnitude(aBlock, bBlock, outputBlock);
    }
}


