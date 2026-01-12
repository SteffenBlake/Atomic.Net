using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise subtraction over one input block and a scalar: output[i] = input[i] - s.
/// </summary>
public sealed class SubtractScalarBlockMap(
    BlockMapBase input,
    FloatMapBase s,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : UnaryAndScalarBlockMapBase(input, s, initValue, dense, blockSize)
{
    protected override void SIMDBlockAndScalar(float[] inputBlock, float s, float[] outputBlock)
    {
        TensorPrimitives.Subtract(inputBlock, s, outputBlock);
    }
}


