using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform position component (x, y, or z).
/// Maps to JSON: { "position": "x" }
/// Applies the value to transform.position.x
/// </summary>
public readonly record struct JsonTransformPositionTarget(string Position)
{
    public void Apply(JsonNode transform, JsonNode value) 
    {
        if (transform is not JsonObject transformObj)
        {
            return;
        }

        // Get or create the "position" object inside transform
        if (!transformObj.TryGetPropertyValue("position", out var positionNode) || positionNode is not JsonObject positionObj)
        {
            positionObj = new JsonObject();
            transformObj["position"] = positionObj;
        }

        // Set the component (x/y/z) inside the position object
        positionObj[Position] = value;
    }
}
