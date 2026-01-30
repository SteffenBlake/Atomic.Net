using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform position component (x, y, or z).
/// Maps to JSON: { "position": "x" }
/// Applies the value to transform.position.x
/// </summary>
public readonly record struct JsonTransformPositionTarget(string Position)
{
    private static readonly HashSet<string> _validFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "x", "y", "z"
    };

    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized position field. Expected one of: x, y, z"
    );

    public void Apply(JsonObject transform, JsonNode value) 
    {
        // Get or create the "position" object inside transform
        if (!transform.TryGetPropertyValue("position", out var positionNode) || positionNode is not JsonObject positionObj)
        {
            positionObj = new JsonObject();
            transform["position"] = positionObj;
        }

        // Validate field name
        if (!_validFields.Contains(Position))
        {
            EventBus<ErrorEvent>.Push(UnrecognizedFieldError);
            return;
        }

        // Set the component (x/y/z) inside the position object
        positionObj[Position] = value;
    }
}
