using System.Text;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Selectors;

public class CollisionEnterEntitySelector(
    int hashcode, EntitySelector? prior = null
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
        stringBuilder.Append("!enter");
    }

    public void MarkDirty() => _dirty = true;

    public bool Recalc()
    {
        var priorDirty = prior?.Recalc() ?? false;
        var shouldRecalc = priorDirty || _dirty;

        if (shouldRecalc)
        {
            // senior-dev: Reset dirty flag even though implementation is pending
            // Stage 1: parsing only, no actual collision matching yet
            _dirty = false;
            
            // senior-dev: In Stage 1, we just clear matches and return
            // Collision registry will be implemented in Stage 2
            Matches.Clear();
        }

        return shouldRecalc;
    }
}

