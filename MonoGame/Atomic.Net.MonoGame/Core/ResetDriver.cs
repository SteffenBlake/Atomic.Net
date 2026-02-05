using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Sequencing;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Driver that resets scene partition across all registries.
/// Replaces ResetEvent-based pattern with direct method calls for thread safety.
/// </summary>
public sealed class ResetDriver : ISingleton<ResetDriver>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
    }

    public static ResetDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Resets scene partition across all registries.
    /// Called by SceneManager on main thread before async scene loading.
    /// </summary>
    public void Run()
    {
        // Reset entity registry first (deactivates scene entities, fires PreEntityDeactivatedEvent)
        EntityRegistry.Instance.Reset();

        // Reset rule and sequence registries (clear scene partition)
        RuleRegistry.Instance.Reset();
        SequenceRegistry.Instance.Reset();

        // Reset selector registry (recalc all selectors after scene entities cleared)
        SelectorRegistry.Instance.Reset();

        // Reset hierarchy registry (clear scene partition)
        HierarchyRegistry.Instance.Reset();

        // Reset entity ID registry (clear scene partition)
        EntityIdRegistry.Instance.Reset();

        // Note: PropertiesRegistry, TagRegistry, TransformRegistry, and DatabaseRegistry
        // have no Reset() methods - they clean up automatically via PreBehaviorRemovedEvent
        // when scene entities are deactivated by EntityRegistry.Reset()
    }
}
