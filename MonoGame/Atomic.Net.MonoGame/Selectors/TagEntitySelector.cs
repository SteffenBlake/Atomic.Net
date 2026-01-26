using System.Text;
using Atomic.Net.MonoGame.Core;

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
            throw new NotImplementedException("Requires Tags registry to be implemented later");
        }

        return shouldRecalc;
    }
}

