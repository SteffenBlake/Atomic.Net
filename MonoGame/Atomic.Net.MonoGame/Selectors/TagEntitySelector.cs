using System.Text;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Tags;

namespace Atomic.Net.MonoGame.Selectors;

public class TagEntitySelector(
    int hashcode, string tag, EntitySelector? prior = null
)
{
    public readonly SparseArray<bool> Matches = new(Constants.MaxEntities);

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
            Matches.Clear();
            
            // Resolve all entities with this tag from TagRegistry
            if (TagRegistry.Instance.TryResolve(tag, out var tagMatches))
            {
                // Iterate all entities that have this tag
                foreach (var (entityIndex, _) in tagMatches)
                {
                    // Intersect with prior selector if present
                    var priorMatches = prior?.Matches.HasValue(entityIndex) ?? true;
                    if (priorMatches)
                    {
                        Matches.Set(entityIndex, true);
                    }
                }
            }
            
            // senior-dev: Reset dirty flag after recalc to prevent unnecessary recomputation
            _dirty = false;
        }

        return shouldRecalc;
    }
}

