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
        // senior-dev: Must check both partitions because selector may match either
        // The TrackChild validation will ensure parent and child are in same partition
        
        // Check global partition first
        var globalEnumerator = ParentSelector.Matches.Global.GetEnumerator();
        if (globalEnumerator.MoveNext())
        {
            // Global sparse array uses uint indices, but PartitionIndex needs ushort for global
            ushort parentIndex = (ushort)globalEnumerator.Current.Index;
            parent = EntityRegistry.Instance[parentIndex];
            return true;
        }
        
        // Then check scene partition
        var sceneEnumerator = ParentSelector.Matches.Scene.GetEnumerator();
        if (sceneEnumerator.MoveNext())
        {
            // Scene sparse array uses uint indices, PartitionIndex also uses uint for scene
            uint parentIndex = sceneEnumerator.Current.Index;
            parent = EntityRegistry.Instance[parentIndex];
            return true;
        }

        parent = default;
        return false;
    }
}
