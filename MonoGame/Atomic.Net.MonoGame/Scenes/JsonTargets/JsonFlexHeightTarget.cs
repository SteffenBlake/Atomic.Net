using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Represents a target for mutating FlexHeight.Value or FlexHeight.Percent.
/// </summary>
public readonly record struct JsonFlexHeightTarget(string FlexHeight)
{
    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized flexHeight field. Expected one of: value, percent"
    );

    private static readonly HashSet<string> _validFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "value", "percent"
    };

    public void Apply(JsonObject jsonEntity, JsonNode value)
    {
        if (!_validFields.Contains(FlexHeight))
        {
            EventBus<ErrorEvent>.Push(UnrecognizedFieldError);
            return;
        }

        // Ensure flexHeight object exists
        if (jsonEntity["flexHeight"] is not JsonObject flexHeight)
        {
            flexHeight = new JsonObject();
            jsonEntity["flexHeight"] = flexHeight;
        }

        flexHeight[FlexHeight] = value;
    }
}
