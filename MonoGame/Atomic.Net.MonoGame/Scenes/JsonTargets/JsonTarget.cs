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
        // Simple flex behaviors (not the complex ones with Value/Percent)
        "flexAlignItems", "flexAlignSelf", "flexBorderBottom", "flexBorderLeft",
        "flexBorderRight", "flexBorderTop", "flexDirection", "flexGrow",
        "flexJustifyContent", "flexMarginBottom", "flexMarginLeft", "flexMarginRight",
        "flexMarginTop", "flexPaddingBottom", "flexPaddingLeft", "flexPaddingRight",
        "flexPaddingTop", "flexPositionType", "flexWrap", "flexZOverride"
    };

    static partial void VariantOf(
        JsonPropertiesTarget properties,
        JsonTransformTarget transform,
        JsonFlexHeightTarget flexHeight,
        JsonFlexWidthTarget flexWidth,
        JsonFlexPositionBottomTarget flexPositionBottom,
        JsonFlexPositionLeftTarget flexPositionLeft,
        JsonFlexPositionRightTarget flexPositionRight,
        JsonFlexPositionTopTarget flexPositionTop,
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
        else if (TryMatch(out JsonFlexHeightTarget flexHeight))
        {
            flexHeight.Apply(entityObj, value);
        }
        else if (TryMatch(out JsonFlexWidthTarget flexWidth))
        {
            flexWidth.Apply(entityObj, value);
        }
        else if (TryMatch(out JsonFlexPositionBottomTarget flexPositionBottom))
        {
            flexPositionBottom.Apply(entityObj, value);
        }
        else if (TryMatch(out JsonFlexPositionLeftTarget flexPositionLeft))
        {
            flexPositionLeft.Apply(entityObj, value);
        }
        else if (TryMatch(out JsonFlexPositionRightTarget flexPositionRight))
        {
            flexPositionRight.Apply(entityObj, value);
        }
        else if (TryMatch(out JsonFlexPositionTopTarget flexPositionTop))
        {
            flexPositionTop.Apply(entityObj, value);
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
