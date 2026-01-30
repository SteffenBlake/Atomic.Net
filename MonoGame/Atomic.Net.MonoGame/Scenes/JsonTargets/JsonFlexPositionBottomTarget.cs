using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Represents a target for mutating FlexPositionBottom.Value or FlexPositionBottom.Percent.
/// </summary>
[JsonConverter(typeof(JsonFlexPositionBottomTargetConverter))]
public readonly record struct JsonFlexPositionBottomTarget(string Field)
{
    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized flexPositionBottom field. Expected one of: value, percent"
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

        // Ensure flexPositionBottom object exists
        if (jsonEntity["flexPositionBottom"] is not JsonObject flexPositionBottom)
        {
            flexPositionBottom = new JsonObject();
            jsonEntity["flexPositionBottom"] = flexPositionBottom;
        }

        flexPositionBottom[Field] = value;
    }
}
