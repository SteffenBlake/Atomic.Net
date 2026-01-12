using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise multiply-add over one input block, a scalar y, and a block addend: output[i] = x[i] * y + addend[i].
/// </summary>
public sealed class MultiplyAddScalarYBlockMap(
    BlockMapBase x,
    FloatMapBase y,
    BlockMapBase addend,
    float initValue = 0,
    bool dense = false,
    ushort blockSize = 16
) : BinaryAndScalarBlockMapBase(x, addend, y, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(
        float[] x, float[] addend, float y, float[] outputBlock
    )
    {
        TensorPrimitives.MultiplyAdd(x, y, addend, outputBlock);
    }
}


