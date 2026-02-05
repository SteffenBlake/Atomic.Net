namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Command to trigger async scene loading.
/// </summary>
public readonly record struct SceneLoadCommand(string SceneLoad)
{
    public readonly string ScenePath => SceneLoad;
}
