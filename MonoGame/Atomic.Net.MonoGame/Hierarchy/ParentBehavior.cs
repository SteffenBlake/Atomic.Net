using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Hierarchy;

public readonly record struct ParentBehavior(EntitySelector ParentSelector)
{
    public bool TryFindParent(
        [NotNullWhen(true)]
        out Entity? parent
    )
    {
        var enumerator = ParentSelector.Matches.GetEnumerator();
        if (enumerator.MoveNext())
        {
            var parentIndex = enumerator.Current.Index;
            parent = EntityRegistry.Instance[parentIndex];
            return true;
        }

        parent = default;
        return false;
    }
}
