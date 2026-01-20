namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Initializes the scene loading system and required registries.
/// </summary>
public static class SceneSystem
{
    public static void Initialize()
    {
        EntityIdRegistry.Initialize();
        SceneLoader.Initialize();
    }
}
