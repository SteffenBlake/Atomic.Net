using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Initializes the scene loading system and required registries.
/// </summary>
public static class SceneSystem
{
    public static void Initialize()
    {
        // senior-dev: Initialize EntityId behavior registry
        BehaviorRegistry<EntityId>.Initialize();
        
        // senior-dev: Initialize Transform behavior registry (needed for scene loading)
        BehaviorRegistry<TransformBehavior>.Initialize();
        
        // senior-dev: EntityIdRegistry self-initializes via singleton pattern
        // senior-dev: Just access Instance to ensure it's created and registered for events
        _ = EntityIdRegistry.Instance;
        
        // senior-dev: Do NOT access SceneLoader.Instance here - circular dependency!
        // senior-dev: SceneLoader will call SceneSystem.Initialize() when it's constructed
    }
}
