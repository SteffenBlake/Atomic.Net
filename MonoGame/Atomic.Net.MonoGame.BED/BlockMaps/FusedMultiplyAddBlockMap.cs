using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise fused multiply-add over three input blocks: output[i] = x[i] * y[i] + addend[i].
/// </summary>
public sealed class FusedMultiplyAddBlockMap(
    BlockMapBase x,
    BlockMapBase y,
    BlockMapBase addend,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : TernaryBlockMapBase(x, y, addend, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] xBlock, float[] yBlock, float[] addendBlock, float[] outputBlock)
    {
        TensorPrimitives.FusedMultiplyAdd(xBlock, yBlock, addendBlock, outputBlock);
    }
}


