using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating an entity's properties behavior.
/// Maps to JSON: { "properties": "propertyKey" }
/// </summary>
public readonly record struct JsonPropertiesTarget(string PropertyKey)
{
    public void Apply(JsonObject jsonEntity, JsonNode value)
    {
        // Get or create the properties object
        if (!jsonEntity.TryGetPropertyValue("properties", out var propertiesNode) || propertiesNode is not JsonObject properties)
        {
            properties = new JsonObject();
            jsonEntity["properties"] = properties;
        }

        properties[PropertyKey] = value;
    }
}
