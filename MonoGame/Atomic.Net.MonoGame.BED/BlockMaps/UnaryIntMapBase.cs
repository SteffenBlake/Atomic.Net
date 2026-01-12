namespace Atomic.Net.MonoGame.BED.BlockMaps;

/// <summary>
/// Base class for unary scalar int block maps that compute a single int value from a single input BlockMap.
/// Handles iterating over input blocks, reducing each block, and aggregating results into the final output.
/// </summary>
public abstract class UnaryIntMapBase(
    BlockMapBase input
) : IntMapBase([input])
{
    private readonly BlockMapBase input = input;
    private readonly int?[] _results = new int?[input.BlockCount];

    protected override void Recompute()
    {
        for (var i = 0; i < input.BlockCount; i++)
        {
            var block = input.RecalculateBlock(i);
            if (block != null)
            {
                _results[i] = ReduceBlock(block);
            }
        }
        Value = Aggregate(
            _results
                .Where(static r => r != null)
                .Cast<int>()
        );
    }

    protected abstract int ReduceBlock(float[] block);
    
    protected abstract int? Aggregate(IEnumerable<int> results);
}
