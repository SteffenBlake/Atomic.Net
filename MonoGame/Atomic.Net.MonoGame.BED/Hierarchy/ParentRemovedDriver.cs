using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Hierarchy;

/// <summary>
/// Handles the removal of a Parent behavior and updates the hierarchy registry accordingly.
/// </summary>
public sealed class ParentRemovedDriver : IEventHandler<BehaviorRemovedEvent<Parent>>, ISingleton<ParentRemovedDriver>
{
    [field: AllowNull]
    public static ParentRemovedDriver Instance => field ??= new();

    public void OnEvent(BehaviorRemovedEvent<Parent> e)
    {
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            HierarchyRegistry.UntrackChild(
                EntityRegistry.Instance[behavior.Value.ParentIndex], 
                e.Entity
            );
        }
    }
}
