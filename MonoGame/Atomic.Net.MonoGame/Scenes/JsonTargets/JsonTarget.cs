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

    static partial void VariantOf(
        JsonPropertiesTarget properties,
        JsonIdTarget id,
        JsonTagsTarget tags,
        JsonParentTarget parent,
        JsonTransformFieldTarget transform,
        JsonFlexTarget flex
    );

    public void Apply(JsonNode jsonEntity, JsonNode value)
    {
        if (TryMatch(out JsonPropertiesTarget properties))
        {
            properties.Apply(jsonEntity, value);
        }
        else if (TryMatch(out JsonIdTarget id))
        {
            id.Apply(jsonEntity, value);
        }
        else if (TryMatch(out JsonTagsTarget tags))
        {
            tags.Apply(jsonEntity, value);
        }
        else if (TryMatch(out JsonParentTarget parent))
        {
            parent.Apply(jsonEntity, value);
        }
        else if (TryMatch(out JsonTransformFieldTarget transform))
        {
            transform.Apply(jsonEntity, value);
        }
        else if (TryMatch(out JsonFlexTarget flex))
        {
            flex.Apply(jsonEntity, value);
        }
        else
        {
            EventBus<ErrorEvent>.Push(UnrecognizedTargetError);
        }
    }
}
