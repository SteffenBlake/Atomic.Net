using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise multiply-add over two input blocks and a scalar addend: output[i] = x[i] * y[i] + addend.
/// </summary>
public sealed class MultiplyAddScalarAddBlockMap(
    BlockMapBase x,
    BlockMapBase y,
    FloatMapBase addend,
    float initValue = 0,
    bool dense = false,
    ushort blockSize = 16
) : BinaryAndScalarBlockMapBase(x, y, addend, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(
        float[] x, float[] y, float addend, float[] outputBlock
    )
    {
        TensorPrimitives.MultiplyAdd(x, y, addend, outputBlock);
    }
}


