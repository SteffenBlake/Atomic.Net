using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Hierarchy;

[JsonConverter(typeof(ParentBehaviorConverter))]
public readonly record struct ParentBehavior(EntitySelector ParentSelector)
{
    public bool TryFindParent(
        [NotNullWhen(true)]
        out Entity? parent
    )
    {
        // senior-dev: Must recalculate selector before accessing Matches
        // Selectors are marked dirty when IDs change, but not automatically recalculated
        ParentSelector.Recalc();
        
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
