using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Represents a target for mutating FlexWidth.Value or FlexWidth.Percent.
/// </summary>
public readonly record struct JsonFlexWidthTarget(string FlexWidth)
{
    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized flexWidth field. Expected one of: value, percent"
    );

    private static readonly HashSet<string> _validFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "value", "percent"
    };

    public void Apply(JsonObject jsonEntity, JsonNode value)
    {
        if (!_validFields.Contains(FlexWidth))
        {
            EventBus<ErrorEvent>.Push(UnrecognizedFieldError);
            return;
        }

        // Ensure flexWidth object exists
        if (jsonEntity["flexWidth"] is not JsonObject flexWidth)
        {
            flexWidth = new JsonObject();
            jsonEntity["flexWidth"] = flexWidth;
        }

        flexWidth[FlexWidth] = value;
    }
}
