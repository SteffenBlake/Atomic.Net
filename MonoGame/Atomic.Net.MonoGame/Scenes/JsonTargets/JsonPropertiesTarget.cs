using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating an entity's properties behavior.
/// Maps to JSON: { "properties": "propertyKey" }
/// </summary>
public readonly record struct JsonPropertiesTarget(string PropertyKey)
{
    public void Apply(JsonNode jsonEntity, JsonNode value)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        // Get or create the properties object
        if (!entityObj.TryGetPropertyValue("properties", out var propertiesNode) || propertiesNode is not JsonObject properties)
        {
            properties = new JsonObject();
            entityObj["properties"] = properties;
        }

        properties[PropertyKey] = value;
    }
}
