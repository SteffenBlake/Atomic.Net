using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Persistence;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Sequencing;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Transform;

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
    /// Called by SceneManager on background thread during async scene loading.
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

        // Reset properties registry (clear scene partition)
        PropertiesRegistry.Instance.Reset();

        // Reset tags registry (clear scene partition)
        TagRegistry.Instance.Reset();

        // Reset transform registry (clear scene partition)
        TransformRegistry.Instance.Reset();

        // Reset database registry (clear scene partition)
        DatabaseRegistry.Instance.Reset();

        // Reset entity ID registry (clear scene partition)
        EntityIdRegistry.Instance.Reset();
    }
}
