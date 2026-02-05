using System.Text.Json.Nodes;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Command that mutates entity properties via an array of operations.
/// Each operation specifies a target path and a value expression.
/// Mutations are applied to JsonNode entity representations.
/// </summary>
public readonly record struct MutCommand(List<MutOperation> Mut)
{
    public readonly IReadOnlyList<MutOperation> Operations => Mut;

    /// <summary>
    /// Executes all mutation operations on a JsonNode entity.
    /// Mutates the jsonEntity in-place based on the context.
    /// </summary>
    /// <param name="jsonEntity">The JSON entity to mutate</param>
    /// <param name="context">The JsonLogic context containing world, entities, and self</param>
    public void Execute(JsonNode jsonEntity, JsonObject context)
    {
        foreach (var operation in Mut)
        {
            JsonNode? result;

            // Optimization: Skip JsonLogic.Apply for literal values (numbers, strings, booleans, null)
            // JsonLogic only needs to run for objects/arrays (complex expressions)
            if (operation.Value is JsonValue)
            {
                // Literal value - use directly without JsonLogic overhead
                result = operation.Value;
            }
            else if (operation.Value is null)
            {
                // Null literal
                result = null;
            }
            else
            {
                // Complex expression (JsonObject or JsonArray) - needs JsonLogic evaluation
                result = JsonLogic.Apply(operation.Value, context);
            }

            if (result == null)
            {
                // Skip this operation if evaluation returned null
                continue;
            }

            // Clone result only if it has a parent to avoid JsonNode parent conflicts
            // JsonLogic.Apply may return nodes that already have parents
            var valueToApply = result.Parent != null ? result.DeepClone() : result;

            // Apply the mutation to the JsonNode entity
            operation.Target.Apply(jsonEntity, valueToApply);
        }
    }
}

