using Atomic.Net.MonoGame.Transform;

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
    }
}
