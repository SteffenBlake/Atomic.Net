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
        // Check global partition first
        var globalEnumerator = ParentSelector.Matches.Global.GetEnumerator();
        if (globalEnumerator.MoveNext())
        {
            var parentIndex = (ushort)globalEnumerator.Current.Index;
            parent = EntityRegistry.Instance[parentIndex];
            return true;
        }
        
        // Then check scene partition
        var sceneEnumerator = ParentSelector.Matches.Scene.GetEnumerator();
        if (sceneEnumerator.MoveNext())
        {
            var parentIndex = (uint)sceneEnumerator.Current.Index;
            parent = EntityRegistry.Instance[parentIndex];
            return true;
        }

        parent = default;
        return false;
    }
}
