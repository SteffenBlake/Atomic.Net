using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise add-multiply over two input blocks and a scalar multiplier: output[i] = (x[i] + y[i]) * multiplier.
/// </summary>
public sealed class AddMultiplyScalarMultiplierBlockMap(
    BlockMapBase x,
    BlockMapBase y,
    FloatMapBase multiplier,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryAndScalarBlockMapBase(x, y, multiplier, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(float[] xBlock, float[] yBlock, float multiplier, float[] outputBlock)
    {
        TensorPrimitives.AddMultiply(xBlock, yBlock, multiplier, outputBlock);
    }
}


