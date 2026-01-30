using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Represents a target for mutating FlexPositionRight.Value or FlexPositionRight.Percent.
/// </summary>
[JsonConverter(typeof(JsonFlexPositionRightTargetConverter))]
public readonly record struct JsonFlexPositionRightTarget(string Field)
{
    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized flexPositionRight field. Expected one of: value, percent"
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

        // Ensure flexPositionRight object exists
        if (jsonEntity["flexPositionRight"] is not JsonObject flexPositionRight)
        {
            flexPositionRight = new JsonObject();
            jsonEntity["flexPositionRight"] = flexPositionRight;
        }

        flexPositionRight[Field] = value;
    }
}
