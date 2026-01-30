using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating an entity's parent.
/// Maps to JSON: "parent"
/// </summary>
public readonly record struct JsonParentTarget()
{
    public void Apply(JsonNode jsonEntity, JsonNode value)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        entityObj["parent"] = value;
    }
}
