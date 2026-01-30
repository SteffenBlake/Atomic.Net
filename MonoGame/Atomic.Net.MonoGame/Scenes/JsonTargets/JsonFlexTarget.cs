using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating flex behavior fields.
/// Maps to JSON string targets like: "flexAlignItems", "flexBorderLeft", etc.
/// </summary>
public readonly record struct JsonFlexTarget(string FieldName)
{
    public void Apply(JsonNode jsonEntity, JsonNode value)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        entityObj[FieldName] = value;
    }
}
