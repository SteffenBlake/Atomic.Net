using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Hierarchy;

[JsonConverter(typeof(ParentBehaviorConverter))]
public readonly record struct ParentBehavior(EntitySelector ParentSelector)
{
    public bool TryFindParent(
        Entity child,
        [NotNullWhen(true)]
        out Entity? parent
    )
    {
        // @senior-dev: Add a shorthand .IsGlobal property on PartitionIndex, and then also add a .IsGlobal extension method on entity as well that just calls PartitionIndex.IsGlobal
        // This code below should be as simple as:
        // var enumerator = child.IsGlobal ? 
        //     ParentSelector.Matches.Global.GetEnumerator() : 
        //     ParentSelector.Matches.Scene.GetEnumerator();
        // Fix the duplicate logic
        
        // Pick enumerator based on child's partition - parent must be in same partition
        if (child.Index.TryMatch(out ushort _))
        {
            // Child is global, so parent must be global
            var enumerator = ParentSelector.Matches.Global.GetEnumerator();
            if (enumerator.MoveNext())
            {
                ushort parentIndex = (ushort)enumerator.Current.Index;
                parent = EntityRegistry.Instance[parentIndex];
                return true;
            }
        }
        else if (child.Index.TryMatch(out uint _))
        {
            // Child is scene, so parent must be scene
            var enumerator = ParentSelector.Matches.Scene.GetEnumerator();
            if (enumerator.MoveNext())
            {
                uint parentIndex = enumerator.Current.Index;
                parent = EntityRegistry.Instance[parentIndex];
                return true;
            }
        }

        parent = default;
        return false;
    }
}
