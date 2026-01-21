namespace Atomic.Net.MonoGame.Scenes.JsonModels;

/// <summary>
/// Root container for scene JSON files.
/// </summary>
public class JsonScene
{
    /// <summary>
    /// List of entities to spawn in the scene.
    /// </summary>
    public List<JsonEntity> Entities { get; set; } = [];
}
