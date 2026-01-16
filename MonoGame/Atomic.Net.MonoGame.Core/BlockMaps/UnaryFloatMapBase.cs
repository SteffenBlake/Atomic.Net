namespace Atomic.Net.MonoGame.Core.BlockMaps;

/// <summary>
/// Base class for unary scalar block maps that compute a single float value from a single input BlockMap.
/// Handles iterating over input blocks, reducing each block, and aggregating results into the final output.
/// </summary>
public abstract class UnaryFloatMapBase(
    BlockMapBase input
) : FloatMapBase([input])
{
    private readonly BlockMapBase input = input;
    private readonly float?[] _results = new float?[input.BlockCount];

    protected override void Recompute()
    {
        for (var i = 0; i < input.BlockCount; i++)
        {
            var block = input.RecalculateBlock(i);
            if (block != null)
            {
                var blockSize = block.Length;
                var filtered = block.Select((value, laneIndex) => {
                    var entityIndex = (ushort)((i * blockSize) + laneIndex);
                    return (value, entityIndex);
                })
                .Where(b => EntityRegistry.Instance.IsActive(EntityRegistry.Instance[b.entityIndex]))
                .Select(b => b.value);

                _results[i] = Aggregate(filtered);
            }
        }

        var filteredResults = _results
            .Where(static r => r != null)
            .Cast<float>();

        if (!filteredResults.Any())
        {
            Value = null;
        }

        Value = Aggregate(filteredResults);
    }

    protected abstract float? Aggregate(IEnumerable<float> results);
}
