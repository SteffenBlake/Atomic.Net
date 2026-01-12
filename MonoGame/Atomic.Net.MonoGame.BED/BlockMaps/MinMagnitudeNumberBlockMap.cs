using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise minimum magnitude number over two input blocks: output[i] = MinMagnitudeNumber(a[i], b[i]).
/// </summary>
public sealed class MinMagnitudeNumberBlockMap(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryBlockMapBase(a, b, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        TensorPrimitives.MinMagnitudeNumber(aBlock, bBlock, outputBlock);
    }
}


