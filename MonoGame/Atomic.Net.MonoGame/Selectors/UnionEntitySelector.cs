using System.Text;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Selectors;

public class UnionEntitySelector(
    int hashcode, List<EntitySelector> children
)
{
    public readonly SparseArray<bool> Matches = new(Constants.MaxEntities);

    public override int GetHashCode() => hashcode;

    public override string ToString()
    {
        var builder = new StringBuilder();
        WriteTo(builder);
        return builder.ToString();
    }

    internal void WriteTo(StringBuilder stringBuilder)
    {
        for (var n = 0; n < children.Count; n++)
        {
            var child = children[n];
            child.WriteTo(stringBuilder);
            if (n != (children.Count -1))
            {
                stringBuilder.Append(',');
            }
        }
    }

    public bool Recalc()
    {
        var shouldRecalc = false;
        foreach(var child in children)
        {
            shouldRecalc |= child.Recalc();
        }

        if (shouldRecalc)
        {
            Matches.Clear();

            foreach(var child in children)
            {
                TensorSparse.Or(Matches, child.Matches, Matches);
            }
        }

        return shouldRecalc;
    }
}
