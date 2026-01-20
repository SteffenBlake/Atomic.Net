using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Singleton scene loader that loads scenes from JSON files.
/// Uses two-pass loading:
///   - Pass 1: Create all entities, apply behaviors (Transform, EntityId)
///   - Pass 2: Resolve parent references, fire error events for unresolved IDs
/// Forces GC after loading completes to prevent lag in game scene.
/// </summary>
public sealed class SceneLoader : ISingleton<SceneLoader>
{
    private static SceneLoader? _instance;
    public static SceneLoader Instance => _instance ??= new SceneLoader();

    private SceneLoader()
    {
    }

    /// <summary>
    /// Loads a game scene from JSON file.
    /// Spawns entities using EntityRegistry.Activate() (indices ≥32).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadGameScene(string scenePath)
    {
        // test-architect: Stub - To be implemented by @senior-dev
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    /// <summary>
    /// Loads a loading scene from JSON file.
    /// Spawns entities using EntityRegistry.ActivateLoading() (indices &lt;32).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadLoadingScene(string scenePath)
    {
        // test-architect: Stub - To be implemented by @senior-dev
        throw new NotImplementedException("To be implemented by @senior-dev");
    }
}
