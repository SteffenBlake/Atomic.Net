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
        // senior-dev: Initialize BehaviorRegistry FIRST (per PR requirements)
        // This ensures PersistToDiskBehavior registry is ready before DatabaseRegistry needs it
        BehaviorRegistry<PersistToDiskBehavior>.Initialize();
        
        // senior-dev: SceneLoader depends on TransformBehavior, so initialize TransformSystem
        TransformSystem.Initialize();
        SceneLoader.Initialize();
        
        // senior-dev: Initialize DatabaseRegistry to register for events
        DatabaseRegistry.Initialize();
    }
}
