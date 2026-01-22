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
        // This ensures PersistToDiskBehavior registry is ready before DatabaseRegistry needs it
        BehaviorRegistry<PersistToDiskBehavior>.Initialize();
        
        TransformSystem.Initialize();
        SceneLoader.Initialize();
        
        DatabaseRegistry.Initialize();
    }
}
