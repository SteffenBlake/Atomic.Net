using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Scenes.Persistence;
using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Initializes the scene loading system and required registries.
/// </summary>
public static class SceneSystem
{
    public static void Initialize()
    {
        // senior-dev: SceneLoader depends on TransformBehavior, so initialize TransformSystem first
        TransformSystem.Initialize();
        SceneLoader.Initialize();
        
        // senior-dev: Initialize PersistToDiskBehavior registry
        BehaviorRegistry<PersistToDiskBehavior>.Initialize();
        
        // senior-dev: Initialize DatabaseRegistry to register for events
        DatabaseRegistry.Initialize();
    }
}
