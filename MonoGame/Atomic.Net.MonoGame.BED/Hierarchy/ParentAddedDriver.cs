using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Hierarchy;

/// <summary>
/// Handles the addition of a Parent behavior and updates the hierarchy registry accordingly.
/// </summary>
public sealed class ParentAddedDriver : IEventHandler<BehaviorAddedEvent<Parent>>, ISingleton<ParentAddedDriver>
{
    [field: AllowNull]
    public static ParentAddedDriver Instance => field ??= new();

    public void OnEvent(BehaviorAddedEvent<Parent> e)
    {
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            HierarchyRegistry.TrackChild(new Entity(behavior.Value.ParentIndex), e.Entity);
        }
    }
}


