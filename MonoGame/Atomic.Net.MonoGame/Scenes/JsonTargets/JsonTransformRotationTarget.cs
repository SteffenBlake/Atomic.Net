using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform rotation component (x, y, z, or w).
/// Maps to JSON: { "rotation": "x" }
/// Applies the value to transform.rotation.x
/// </summary>
public readonly record struct JsonTransformRotationTarget(string Rotation)
{
    public void Apply(JsonNode transform, JsonNode value) 
    {
        if (transform is not JsonObject transformObj)
        {
            return;
        }

        // Get or create the "rotation" object inside transform
        if (!transformObj.TryGetPropertyValue("rotation", out var rotationNode) || rotationNode is not JsonObject rotationObj)
        {
            rotationObj = new JsonObject();
            transformObj["rotation"] = rotationObj;
        }

        // Set the component (x/y/z/w) inside the rotation object
        rotationObj[Rotation] = value;
    }
}
