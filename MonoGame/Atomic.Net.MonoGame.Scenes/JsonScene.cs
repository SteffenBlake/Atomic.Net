namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Root container for scene JSON files.
/// </summary>
public class JsonScene
{
    /// <summary>
    /// List of entities to spawn in the scene.
    /// </summary>
    public List<JsonEntity> Entities { get; set; } = [];
    
    /// <summary>
    /// List of rules to spawn in the scene, optional.
    /// </summary>
    public List<JsonRule>? Rules { get; set; } = null;
}
