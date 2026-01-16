using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Hierarchy;

/// <summary>
/// Handles the pre-update event of a Parent behavior.
/// Ensures the child is removed from its old parent in the hierarchy registry before the update.
/// </summary>
public sealed class ParentPreUpdatedDriver : IEventHandler<PreBehaviorUpdatedEvent<Parent>>, ISingleton<ParentPreUpdatedDriver>
{
    [field: AllowNull]
    public static ParentPreUpdatedDriver Instance => field ??= new();

    public void OnEvent(PreBehaviorUpdatedEvent<Parent> e)
    {
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(e.Entity, out var oldBehavior))
        {
            HierarchyRegistry.UntrackChild(
                EntityRegistry.Instance[oldBehavior.Value.ParentIndex], 
                e.Entity
            );
        }
    }
}


