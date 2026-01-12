namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Computes the average of all values across the blocks of the input BlockMap and outputs a single float.
/// Iterates over each block, reduces it to a block-average, and aggregates those into the final result.
/// </summary>
public sealed class AverageFloatMap(
    BlockMapBase input
) : UnaryFloatMapBase(input)
{
    protected override float? Aggregate(IEnumerable<float> results)
    {
        return results.Average();
    }
}
