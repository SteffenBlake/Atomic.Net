using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Applies the softmax function to each element in the input block and stores the result in this block.
/// </summary>
public sealed class SoftMaxMapBlock(
    BlockMapBase input,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : UnaryBlockMapBase(input, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] inputBlock, float[] outputBlock)
    {
        TensorPrimitives.SoftMax(inputBlock, outputBlock);
    }
}


