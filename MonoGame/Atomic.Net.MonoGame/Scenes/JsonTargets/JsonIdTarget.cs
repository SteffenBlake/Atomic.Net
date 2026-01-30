using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating an entity's ID.
/// Maps to JSON: "id"
/// </summary>
public readonly record struct JsonIdTarget()
{
    public void Apply(JsonNode jsonEntity, JsonNode value)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        entityObj["id"] = value;
    }
}
