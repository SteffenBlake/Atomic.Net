using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Represents a target for mutating FlexHeight.Value or FlexHeight.Percent.
/// </summary>
[JsonConverter(typeof(JsonFlexHeightTargetConverter))]
public readonly record struct JsonFlexHeightTarget(string Field)
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
        if (!_validFields.Contains(Field))
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

        flexHeight[Field] = value;
    }
}
