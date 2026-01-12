namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Base class for block maps that compute element-wise values from two input blocks and a scalar value.
/// The scalar value is exposed via property <see cref="S"/> and triggers dirty propagation when modified.
/// </summary>
public abstract class BinaryAndScalarBlockMapBase : BinaryBlockMapBase
{
    private readonly FloatMapBase _s;

    public BinaryAndScalarBlockMapBase(
        BlockMapBase a,
        BlockMapBase b,
        FloatMapBase s,
        float initValue = 0.0f,
        bool dense = false,
        ushort blockSize = 16
    ) : base(a, b, initValue, dense, blockSize)
    {
        _s = s;
        _s.OnDirty += MakeDirty;
        MakeDirtyAllBlocks();
    }

    protected override void SIMDBlock(float[] aBlock, float[] bBlock, float[] outputBlock)
    {
        var sValue = _s.Recalculate();
        if (!sValue.HasValue)
        {
            return;
        }

        SIMDBlockAndScalar(aBlock, bBlock, sValue.Value, outputBlock);
    }

    protected abstract void SIMDBlockAndScalar(float[] aBlock, float[] bBlock, float s, float[] outputBlock);

    private void MakeDirtyAllBlocks()
    {
        for (int i = 0; i < _outputs.Length; i++)
        {
            MakeDirty(i);
        }
    }
}


