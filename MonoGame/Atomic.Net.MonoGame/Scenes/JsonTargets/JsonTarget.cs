using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;
using dotVariant;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Top-level variant for all possible mutation targets.
/// Handles applying mutations to JSON entity representations.
/// </summary>
[Variant]
[JsonConverter(typeof(JsonTargetConverter))]
public partial struct JsonTarget
{
    private static readonly ErrorEvent UnrecognizedTargetError = new(
        "Unrecognized target type"
    );

    private static readonly HashSet<string> _validLeaves = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "tags", "parent",
        // All flex behaviors
        "flexAlignItems", "flexAlignSelf", "flexBorderBottom", "flexBorderLeft",
        "flexBorderRight", "flexBorderTop", "flexDirection", "flexGrow",
        "flexHeight", "flexHeightPercent", "flexJustifyContent", "flexMarginBottom",
        "flexMarginLeft", "flexMarginRight", "flexMarginTop", "flexPaddingBottom",
        "flexPaddingLeft", "flexPaddingRight", "flexPaddingTop", "flexPositionBottom",
        "flexPositionBottomPercent", "flexPositionLeft", "flexPositionLeftPercent",
        "flexPositionRight", "flexPositionRightPercent", "flexPositionTop",
        "flexPositionTopPercent", "flexPositionType", "flexWidth", "flexWidthPercent",
        "flexWrap", "flexZOverride"
    };

    static partial void VariantOf(
        JsonPropertiesTarget properties,
        JsonTransformTarget transform,
        string leaf
    );

    public void Apply(JsonNode jsonEntity, JsonNode value)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity JSON must be a JsonObject"));
            return;
        }

        if (TryMatch(out JsonPropertiesTarget properties))
        {
            properties.Apply(entityObj, value);
        }
        else if (TryMatch(out JsonTransformTarget transform))
        {
            transform.Apply(entityObj, value);
        }
        else if (TryMatch(out string? leaf))
        {
            if (_validLeaves.Contains(leaf))
            {
                entityObj[leaf] = value;
                return;
            }
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unrecognized leaf target: '{leaf}'. Expected one of: {string.Join(", ", _validLeaves)}"
            ));
        }
        else
        {
            EventBus<ErrorEvent>.Push(UnrecognizedTargetError);
        }
    }
}
