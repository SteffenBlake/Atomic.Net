using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;
using dotVariant;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Variant for different transform targets (position, rotation, scale, anchor).
/// </summary>
[Variant]
[JsonConverter(typeof(JsonTransformTargetConverter))]
public partial struct JsonTransformTarget
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

    public void Apply(JsonObject jsonEntity, JsonNode value)
    {
        // Get or create transform object
        if (!jsonEntity.TryGetPropertyValue("transform", out var transformNode) || transformNode is not JsonObject transform)
        {
            transform = new JsonObject();
            jsonEntity["transform"] = transform;
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
