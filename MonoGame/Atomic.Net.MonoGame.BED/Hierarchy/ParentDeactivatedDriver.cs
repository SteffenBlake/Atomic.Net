using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Hierarchy;

/// <summary>
/// Handles the removal of a possible Parent entity and updates the behaviors of its children accordingly
/// </summary>
public sealed class ParentDeactivatedDriver : IEventHandler<EntityDeactivatedEvent>, ISingleton<ParentDeactivatedDriver>
{
    public static ParentDeactivatedDriver Instance { get; } = new();

    public void OnEvent(EntityDeactivatedEvent e)
    {
        foreach (var child in HierarchyRegistry.GetChildren(e.Entity))
        {
            BehaviorRegistry<Parent>.Instance.Remove(child);
        }
    }
}

