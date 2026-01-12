using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise add-multiply over one input block, a scalar y, and a block multiplier: output[i] = (x[i] + y) * multiplier[i].
/// </summary>
public sealed class AddMultiplyScalarYBlockMap(
    BlockMapBase x,
    FloatMapBase y,
    BlockMapBase multiplier,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryAndScalarBlockMapBase(x, multiplier, y, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(float[] xBlock, float[] multiplierBlock, float y, float[] outputBlock)
    {
        TensorPrimitives.AddMultiply(xBlock, y, multiplierBlock, outputBlock);
    }
}


