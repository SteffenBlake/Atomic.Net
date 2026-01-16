using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise subtraction of input from scalar: output[i] = s - input[i].
/// </summary>
public sealed class ScalarSubtractBlockMap(
    FloatMapBase s,
    BlockMapBase input,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : UnaryAndScalarBlockMapBase(input, s, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(float[] inputBlock, float s, float[] outputBlock)
    {
        TensorPrimitives.Subtract(s, inputBlock, outputBlock);
    }
}
