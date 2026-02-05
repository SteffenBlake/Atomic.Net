using System.Text;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;

namespace Atomic.Net.MonoGame.Selectors;

public class IdEntitySelector(
    int hashcode, string id, EntitySelector? prior = null
)
{
    public readonly PartitionedSparseArray<bool> Matches = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    private bool _dirty = true;

    public override int GetHashCode() => hashcode;

    public override string ToString()
    {
        var builder = new StringBuilder();
        WriteTo(builder);
        return builder.ToString();
    }

    internal void WriteTo(StringBuilder stringBuilder)
    {
        // senior-dev: Print prior first (left), then self (right) to match input order
        if (prior != null)
        {
            prior.WriteTo(stringBuilder);
            stringBuilder.Append(':');
        }
        stringBuilder.Append('@');
        stringBuilder.Append(id);
    }

    public void MarkDirty() => _dirty = true;

    public bool Recalc()
    {
        var priorDirty = prior?.Recalc() ?? false;
        var shouldRecalc = priorDirty || _dirty;

        if (shouldRecalc)
        {
            Matches.Global.Clear();
            Matches.Scene.Clear();

            if (EntityIdRegistry.Instance.TryResolve(id, out var match))
            {
                var priorMatches = prior?.Matches.HasValue(match.Value.Index) ?? true;
                if (priorMatches)
                {
                    Matches.Set(match.Value.Index, true);
                }
            }

            // senior-dev: Reset dirty flag after recalc to prevent unnecessary recomputation
            _dirty = false;
        }

        return shouldRecalc;
    }
}

