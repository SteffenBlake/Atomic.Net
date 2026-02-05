using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform rotation component (x, y, z, or w).
/// Maps to JSON: { "rotation": "x" }
/// Applies the value to transform.rotation.x
/// </summary>
public readonly record struct JsonTransformRotationTarget(string Rotation)
{
    private static readonly HashSet<string> _validFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "x", "y", "z", "w"
    };

    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized rotation field. Expected one of: x, y, z, w"
    );

    public void Apply(JsonObject transform, JsonNode value)
    {
        // Get or create the "rotation" object inside transform
        if (!transform.TryGetPropertyValue("rotation", out var rotationNode) || rotationNode is not JsonObject rotationObj)
        {
            rotationObj = new JsonObject();
            transform["rotation"] = rotationObj;
        }

        // Validate field name
        if (!_validFields.Contains(Rotation))
        {
            EventBus<ErrorEvent>.Push(UnrecognizedFieldError);
            return;
        }

        // Set the component (x/y/z/w) inside the rotation object
        rotationObj[Rotation] = value;
    }
}
