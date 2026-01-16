using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise fused multiply-add over one input block, a scalar y, and a block addend: output[i] = x[i] * y + addend[i].
/// </summary>
public sealed class FusedMultiplyAddScalarYBlockMap(
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
        TensorPrimitives.FusedMultiplyAdd(xBlock, y, addendBlock, outputBlock);
    }
}


