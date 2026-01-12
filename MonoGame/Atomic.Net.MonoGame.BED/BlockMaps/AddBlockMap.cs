using System.Numerics.Tensors;

namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes element-wise addition over two input blocks: output[i] = a[i] + b[i].
/// </summary>
public sealed class AddBlockMap(
    BlockMapBase a,
    BlockMapBase b,
    float initValue = 0.0f,
    bool dense = false,
    ushort blockSize = 16
) : BinaryBlockMapBase(a, b, initValue, dense, blockSize)
{
    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        TensorPrimitives.Add(aBlock, bBlock, outputBlock);
    }
}


