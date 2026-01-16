using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Hierarchy;

/// <summary>
/// Handles the removal of a possible Parent entity and updates the behaviors of its children accordingly
/// </summary>
public sealed class ParentDeactivatedDriver : IEventHandler<EntityDeactivatedEvent>, ISingleton<ParentDeactivatedDriver>
{
    [field: AllowNull]
    public static ParentDeactivatedDriver Instance => field ??= new();

    public void OnEvent(EntityDeactivatedEvent e)
    {
        foreach (var child in HierarchyRegistry.GetChildren(e.Entity))
        {
            BehaviorRegistry<Parent>.Instance.Remove(child);
        }
    }
}

