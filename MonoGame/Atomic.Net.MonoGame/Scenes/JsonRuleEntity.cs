using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Tags;
using System.Text.Json.Serialization;
using System.Collections.Immutable;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// JSON model for entities in rule evaluation context.
/// Similar to JsonEntity but includes _index field required for mutations.
/// Reference type is acceptable as these are ephemeral (frame-time only).
/// </summary>
public class JsonRuleEntity
{
    /// <summary>
    /// Entity index in EntityRegistry. Required for applying mutations back to entities.
    /// </summary>
    [JsonPropertyName("_index")]
    public ushort Index { get; set; }
    
    /// <summary>
    /// Optional entity ID for referencing (e.g., "player", "main-menu").
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; } = null;
    
    /// <summary>
    /// Optional tags for group selection in rules and queries (e.g., ["enemy", "boss"]).
    /// </summary>
    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; } = null;

    /// <summary>
    /// Optional transform behavior.
    /// </summary>
    [JsonPropertyName("transform")]
    public TransformBehavior? Transform { get; set; } = null;

    /// <summary>
    /// Optional properties behavior (arbitrary key-value metadata).
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, object?>? Properties { get; set; } = null;

    /// <summary>
    /// Reads entity data from an existing entity by reading all its behaviors.
    /// Used for serialization to JsonLogic context.
    /// </summary>
    public static JsonRuleEntity FromEntity(Entity entity)
    {
        // senior-dev: Create JsonRuleEntity from an existing entity
        var jsonEntity = new JsonRuleEntity
        {
            Index = entity.Index
        };
        
        // senior-dev: Read Id if present
        if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity, out var idBehavior))
        {
            jsonEntity.Id = idBehavior.Value.Id;
        }
        
        // senior-dev: Read Tags if present
        if (BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity, out var tagsBehavior))
        {
            jsonEntity.Tags = tagsBehavior.Value.Tags.ToArray();
        }

        // senior-dev: Read Transform if present
        if (BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transformBehavior))
        {
            jsonEntity.Transform = transformBehavior.Value;
        }

        // senior-dev: Read Properties if present
        if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var propertiesBehavior))
        {
            // senior-dev: Convert PropertiesBehavior to Dictionary for JSON serialization
            var props = propertiesBehavior.Value.Properties;
            if (props != null)
            {
                jsonEntity.Properties = new Dictionary<string, object?>();
                foreach (var kvp in props)
                {
                    // senior-dev: Convert PropertyValue to object
                    var value = kvp.Value.Visit(
                        s => (object?)s,
                        f => (object?)f,
                        b => (object?)b,
                        () => null
                    );
                    jsonEntity.Properties[kvp.Key] = value;
                }
            }
        }

        return jsonEntity;
    }
}
