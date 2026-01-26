using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Hierarchy;

[JsonConverter(typeof(ParentBehaviorConverter))]
public readonly record struct ParentBehavior(EntitySelector ParentSelector) : IBehavior<ParentBehavior>
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

    public static ParentBehavior CreateFor(Entity entity)
    {
        return default;
    }
}
