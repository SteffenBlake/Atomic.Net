using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Hierarchy;

[JsonConverter(typeof(ParentBehaviorConverter))]
public readonly record struct ParentBehavior(EntitySelector ParentSelector)
{
    public bool TryFindParent(
        bool isGlobal,
        [NotNullWhen(true)]
        out Entity? parent
    )
    {
        // Pick enumerator based on child's partition - parent must be in same partition
        var enumerator = isGlobal ?
            ParentSelector.Matches.Global.GetEnumerator() :
            ParentSelector.Matches.Scene.GetEnumerator();

        if (enumerator.MoveNext())
        {
            if (isGlobal)
            {
                ushort parentIndex = (ushort)enumerator.Current.Index;
                parent = EntityRegistry.Instance[parentIndex];
            }
            else
            {
                uint parentIndex = enumerator.Current.Index;
                parent = EntityRegistry.Instance[parentIndex];
            }
            return true;
        }

        parent = default;
        return false;
    }
}
