using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using dotVariant;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Variant for different transform field targets (position, rotation, scale, anchor).
/// </summary>
[Variant]
public partial struct JsonTransformFieldTarget
{
    private static readonly ErrorEvent UnrecognizedFieldError = new(
        "Unrecognized transform field. Expected one of: position, rotation, scale, anchor"
    );

    static partial void VariantOf(
        JsonTransformPositionTarget position,
        JsonTransformRotationTarget rotation,
        JsonTransformScaleTarget scale,
        JsonTransformAnchorTarget anchor
    );

    public void Apply(JsonNode jsonEntity, JsonNode value)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        // Get or create transform object
        if (!entityObj.TryGetPropertyValue("transform", out var transformNode) || transformNode is not JsonObject transform)
        {
            transform = new JsonObject();
            entityObj["transform"] = transform;
        }

        if (TryMatch(out JsonTransformPositionTarget position))
        {
            position.Apply(transform, value);
        }
        else if (TryMatch(out JsonTransformRotationTarget rotation))
        {
            rotation.Apply(transform, value);
        }
        else if (TryMatch(out JsonTransformScaleTarget scale))
        {
            scale.Apply(transform, value);
        }
        else if (TryMatch(out JsonTransformAnchorTarget anchor))
        {
            anchor.Apply(transform, value);
        }
        else
        {
            EventBus<ErrorEvent>.Push(UnrecognizedFieldError);
        }
    }
}
