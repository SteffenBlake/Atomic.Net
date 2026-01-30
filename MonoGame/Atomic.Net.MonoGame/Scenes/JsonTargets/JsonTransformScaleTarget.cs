using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform scale component (x, y, or z).
/// Maps to JSON: { "scale": "x" }
/// Applies the value to transform.scale.x
/// </summary>
public readonly record struct JsonTransformScaleTarget(string Scale)
{
    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized scale field. Expected one of: x, y, z"
    );

    public void Apply(JsonObject transform, JsonNode value) 
    {
        // Get or create the "scale" object inside transform
        if (!transform.TryGetPropertyValue("scale", out var scaleNode) || scaleNode is not JsonObject scaleObj)
        {
            scaleObj = new JsonObject();
            transform["scale"] = scaleObj;
        }

        // Validate field name
        if (Scale != "x" && Scale != "y" && Scale != "z")
        {
            EventBus<ErrorEvent>.Push(UnrecognizedFieldError);
            return;
        }

        // Set the component (x/y/z) inside the scale object
        scaleObj[Scale] = value;
    }
}
