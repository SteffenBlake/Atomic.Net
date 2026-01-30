using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform position component (x, y, or z).
/// Maps to JSON: { "position": "x" }
/// </summary>
public readonly record struct JsonTransformPositionTarget(string Position)
{
    public void Apply(JsonNode transform, JsonNode value) 
    {
        if (transform is not JsonObject transformObj)
        {
            return;
        }

        transformObj[Position] = value;
    }
}
