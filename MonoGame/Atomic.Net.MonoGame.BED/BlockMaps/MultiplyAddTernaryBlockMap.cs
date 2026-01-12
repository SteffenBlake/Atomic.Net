using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise multiply-add over three input blocks: output[i] = x[i] * y[i] + addend[i].
/// </summary>
public sealed class MultiplyAddTernaryBlockMap(
    BlockMapBase x,
    BlockMapBase y,
    BlockMapBase addend,
    float initValue = 0,
    bool dense = false,
    ushort blockSize = 16
) : TernaryBlockMapBase(x, y, addend, initValue, dense, blockSize)
{
    protected override void SIMDBlock(
        float[] x, float[] y, float[] addend, float[] outputBlock
    )
    {
        TensorPrimitives.MultiplyAdd(x, y, addend, outputBlock);
    }
}


