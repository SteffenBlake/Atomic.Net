using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating an entity's tags.
/// Maps to JSON: "tags"
/// </summary>
public readonly record struct JsonTagsTarget()
{
    public void Apply(JsonNode jsonEntity, JsonNode value)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        entityObj["tags"] = value;
    }
}
