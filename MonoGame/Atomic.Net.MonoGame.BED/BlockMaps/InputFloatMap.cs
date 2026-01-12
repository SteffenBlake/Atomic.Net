namespace Atomic.Net.MonoGame.BED.BlockMaps;

public sealed class InputFloatMap(
    float? initValue = null
) : FloatMapBase([], initValue)
{
    protected override void Recompute(){}

    public void SetValue(float value)
    {
        if (Value == value)
        {
            return;
        }

        Value = value;
        MakeDirty();
    }
}

