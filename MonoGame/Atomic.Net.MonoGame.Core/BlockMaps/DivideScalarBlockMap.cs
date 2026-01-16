using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Computes element-wise division over one input block and a scalar: output[i] = input[i] / s.
/// </summary>
public sealed class DivideScalarBlockMap(
    BlockMapBase input,
    FloatMapBase s,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : UnaryAndScalarBlockMapBase(input, s, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(float[] inputBlock, float s, float[] outputBlock)
    {
        TensorPrimitives.Divide(inputBlock, s, outputBlock);
    }
}


