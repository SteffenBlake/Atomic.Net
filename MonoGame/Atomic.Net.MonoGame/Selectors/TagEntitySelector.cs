using System.Text;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Tags;

namespace Atomic.Net.MonoGame.Selectors;

public class TagEntitySelector(
    int hashcode, string tag, EntitySelector? prior = null
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
        stringBuilder.Append('#');
        stringBuilder.Append(tag);
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

            // Resolve all entities with this tag from TagRegistry
            if (TagRegistry.Instance.TryResolve(tag, out var tagMatches))
            {
                if (prior != null)
                {
                    // senior-dev: Use TensorSparse for fast SIMD intersection
                    TensorSparse.And(tagMatches, prior.Matches, Matches);
                }
                else
                {
                    // No prior selector, copy all tag matches
                    foreach (var (entityIndex, _) in tagMatches.Global)
                    {
                        Matches.Set((ushort)entityIndex, true);
                    }
                    foreach (var (entityIndex, _) in tagMatches.Scene)
                    {
                        Matches.Set((uint)entityIndex, true);
                    }
                }
            }

            // senior-dev: Reset dirty flag after recalc to prevent unnecessary recomputation
            _dirty = false;
        }

        return shouldRecalc;
    }
}

