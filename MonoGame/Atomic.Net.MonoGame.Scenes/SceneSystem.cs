using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.BED.Persistence;

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
        
        // senior-dev: Register serialization delegates with DatabaseRegistry
        // This allows DatabaseRegistry (in BED) to serialize/deserialize entities without depending on Scenes
        DatabaseRegistry.Instance.SerializeEntity = SceneLoader.SerializeEntityToJson;
        DatabaseRegistry.Instance.DeserializeEntity = SceneLoader.DeserializeEntityFromJson;
    }
}
