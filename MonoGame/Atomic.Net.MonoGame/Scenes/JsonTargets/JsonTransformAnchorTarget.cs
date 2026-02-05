using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform anchor component (x, y, or z).
/// Maps to JSON: { "anchor": "x" }
/// Applies the value to transform.anchor.x
/// </summary>
public readonly record struct JsonTransformAnchorTarget(string Anchor)
{
    private static readonly HashSet<string> _validFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "x", "y", "z"
    };

    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized anchor field. Expected one of: x, y, z"
    );

    public void Apply(JsonObject transform, JsonNode value)
    {
        // Get or create the "anchor" object inside transform
        if (!transform.TryGetPropertyValue("anchor", out var anchorNode) || anchorNode is not JsonObject anchorObj)
        {
            anchorObj = new JsonObject();
            transform["anchor"] = anchorObj;
        }

        // Validate field name
        if (!_validFields.Contains(Anchor))
        {
            EventBus<ErrorEvent>.Push(UnrecognizedFieldError);
            return;
        }

        // Set the component (x/y/z) inside the anchor object
        anchorObj[Anchor] = value;
    }
}
