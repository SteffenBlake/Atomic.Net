using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Represents a target for mutating FlexPositionTop.Value or FlexPositionTop.Percent.
/// </summary>
public readonly record struct JsonFlexPositionTopTarget(string FlexPositionTop)
{
    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized flexPositionTop field. Expected one of: value, percent"
    );

    private static readonly HashSet<string> _validFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "value", "percent"
    };

    public void Apply(JsonObject jsonEntity, JsonNode value)
    {
        if (!_validFields.Contains(FlexPositionTop))
        {
            EventBus<ErrorEvent>.Push(UnrecognizedFieldError);
            return;
        }

        // Ensure flexPositionTop object exists
        if (jsonEntity["flexPositionTop"] is not JsonObject flexPositionTop)
        {
            flexPositionTop = new JsonObject();
            jsonEntity["flexPositionTop"] = flexPositionTop;
        }

        flexPositionTop[FlexPositionTop] = value;
    }
}
