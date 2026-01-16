using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise add-multiply over three input blocks: output[i] = (x[i] + y[i]) * multiplier[i].
/// </summary>
public sealed class AddMultiplyBlockMap(
    BlockMapBase x,
    BlockMapBase y,
    BlockMapBase multiplier,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : TernaryBlockMapBase(x, y, multiplier, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] xBlock, float[] yBlock, float[] multiplierBlock, float[] outputBlock)
    {
        TensorPrimitives.AddMultiply(xBlock, yBlock, multiplierBlock, outputBlock);
    }
}


