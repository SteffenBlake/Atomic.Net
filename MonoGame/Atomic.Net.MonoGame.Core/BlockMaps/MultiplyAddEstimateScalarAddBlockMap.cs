using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise approximate multiply-add over two input blocks and a scalar addend: output[i] = x[i] * y[i] + addend (estimate).
/// </summary>
public sealed class MultiplyAddEstimateScalarAddBlockMap(
    BlockMapBase x,
    BlockMapBase y,
    FloatMapBase addend,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryAndScalarBlockMapBase(x, y, addend, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(float[] xBlock, float[] yBlock, float addend, float[] outputBlock)
    {
        TensorPrimitives.MultiplyAddEstimate(xBlock, yBlock, addend, outputBlock);
    }
}


