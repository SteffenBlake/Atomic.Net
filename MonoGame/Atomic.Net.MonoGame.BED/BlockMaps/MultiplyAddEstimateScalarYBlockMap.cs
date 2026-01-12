using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise approximate multiply-add over one input block, a scalar y, and a block addend: output[i] = x[i] * y + addend[i] (estimate).
/// </summary>
public sealed class MultiplyAddEstimateScalarYBlockMap(
    BlockMapBase x,
    FloatMapBase y,
    BlockMapBase addend,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryAndScalarBlockMapBase(x, addend, y, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(float[] xBlock, float[] addendBlock, float y, float[] outputBlock)
    {
        TensorPrimitives.MultiplyAddEstimate(xBlock, y, addendBlock, outputBlock);
    }
}


