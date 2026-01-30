using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Represents a target for mutating FlexPositionLeft.Value or FlexPositionLeft.Percent.
/// </summary>
[JsonConverter(typeof(JsonFlexPositionLeftTargetConverter))]
public readonly record struct JsonFlexPositionLeftTarget(string Field)
{
    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized flexPositionLeft field. Expected one of: value, percent"
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

        // Ensure flexPositionLeft object exists
        if (jsonEntity["flexPositionLeft"] is not JsonObject flexPositionLeft)
        {
            flexPositionLeft = new JsonObject();
            jsonEntity["flexPositionLeft"] = flexPositionLeft;
        }

        flexPositionLeft[Field] = value;
    }
}
