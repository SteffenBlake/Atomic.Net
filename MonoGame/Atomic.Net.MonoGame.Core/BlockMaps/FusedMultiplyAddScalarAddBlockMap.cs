using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise fused multiply-add over two input blocks and a scalar addend: output[i] = x[i] * y[i] + addend.
/// </summary>
public sealed class FusedMultiplyAddScalarAddBlockMap(
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
        TensorPrimitives.FusedMultiplyAdd(xBlock, yBlock, addend, outputBlock);
    }
}


