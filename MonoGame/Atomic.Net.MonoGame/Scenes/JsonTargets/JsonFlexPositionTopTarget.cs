using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Represents a target for mutating FlexPositionTop.Value or FlexPositionTop.Percent.
/// </summary>
[JsonConverter(typeof(JsonFlexPositionTopTargetConverter))]
public readonly record struct JsonFlexPositionTopTarget(string Field)
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
        if (!_validFields.Contains(Field))
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

        flexPositionTop[Field] = value;
    }
}
