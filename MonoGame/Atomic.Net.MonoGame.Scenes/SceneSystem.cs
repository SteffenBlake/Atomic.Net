namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Initializes the scene loading system and required registries.
/// </summary>
public static class SceneSystem
{
    public static void Initialize()
    {
        // senior-dev: EntityIdRegistry now initialized in BEDSystem (core behavior registry)
        SceneLoader.Initialize();
    }
}
