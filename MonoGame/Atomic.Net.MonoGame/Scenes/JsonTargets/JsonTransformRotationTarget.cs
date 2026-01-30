using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform rotation component (x, y, z, or w).
/// Maps to JSON: { "rotation": "x" }
/// </summary>
public readonly record struct JsonTransformRotationTarget(string Rotation)
{
    public void Apply(JsonNode transform, JsonNode value) 
    {
        if (transform is not JsonObject transformObj)
        {
            return;
        }

        transformObj[Rotation] = value;
    }
}
