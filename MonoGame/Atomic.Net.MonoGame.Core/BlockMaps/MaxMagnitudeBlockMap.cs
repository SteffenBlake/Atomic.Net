using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise maximum magnitude over two input blocks: output[i] = MaxMagnitude(a[i], b[i]).
/// </summary>
public sealed class MaxMagnitudeBlockMap(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryBlockMapBase(a, b, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        TensorPrimitives.MaxMagnitude(aBlock, bBlock, outputBlock);
    }
}


