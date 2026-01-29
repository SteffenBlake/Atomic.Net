using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Driver for executing mutation commands.
/// Mutates entity JsonNode in-place for both RulesDriver and SequenceDriver.
/// </summary>
public static class MutCmdDriver
{
    /// <summary>
    /// Executes a MutCommand, mutating the entity JsonNode in-place.
    /// Also applies to the actual entity immediately for RulesDriver backwards compatibility.
    /// </summary>
    /// <param name="command">The mutation command to execute.</param>
    /// <param name="context">JsonNode context for evaluating mutation values.</param>
    /// <param name="entityJson">The entity JsonObject to mutate in-place.</param>
    /// <param name="entityIndex">The index of the entity.</param>
    /// <param name="applyToEntity">If true, also applies mutations to actual entity immediately (for RulesDriver).</param>
    public static void Execute(MutCommand command, JsonNode context, JsonObject entityJson, ushort entityIndex, bool applyToEntity = true)
    {
        foreach (var operation in command.Operations)
        {
            JsonNode? computedValue;
            try
            {
                computedValue = JsonLogic.Apply(operation.Value, context);
            }
            catch (Exception ex)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to evaluate mutation value for entity {entityIndex}: {ex.Message}"
                ));
                continue;
            }

            if (computedValue == null)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Mutation value evaluation returned null for entity {entityIndex}"
                ));
                continue;
            }

            // Apply mutation to entity JsonNode
            ApplyMutationToJsonNode(operation.Target, computedValue, entityJson, entityIndex);
            
            // Also apply to actual entity if requested (for RulesDriver backwards compatibility)
            if (applyToEntity)
            {
                MutationHelper.ApplyToTarget(entityIndex, operation.Target, computedValue);
            }
        }
    }

    /// <summary>
    /// Applies a mutation to the entity JsonNode in-place.
    /// </summary>
    private static void ApplyMutationToJsonNode(JsonNode target, JsonNode computedValue, JsonObject entityJson, ushort entityIndex)
    {
        if (target is not JsonObject targetObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Target is not a JsonObject for entity {entityIndex}"
            ));
            return;
        }

        // Handle property mutations
        if (targetObj.TryGetPropertyValue("properties", out var propertyKeyNode) && propertyKeyNode != null)
        {
            var propertyKey = propertyKeyNode.GetValue<string>();
            
            // Get or create properties object
            if (!entityJson.TryGetPropertyValue("properties", out var propsNode) || propsNode is not JsonObject propertiesObj)
            {
                propertiesObj = new JsonObject();
                entityJson["properties"] = propertiesObj;
            }
            else
            {
                propertiesObj = (JsonObject)propsNode;
            }
            
            // Clone the computed value to avoid parent conflicts
            JsonNode? valueToAssign;
            if (computedValue is JsonValue jsonVal)
            {
                // For JsonValue, recreate to avoid parent issues
                if (jsonVal.TryGetValue<string>(out var strVal))
                {
                    valueToAssign = JsonValue.Create(strVal);
                }
                else if (jsonVal.TryGetValue<float>(out var floatVal))
                {
                    valueToAssign = JsonValue.Create(floatVal);
                }
                else if (jsonVal.TryGetValue<bool>(out var boolVal))
                {
                    valueToAssign = JsonValue.Create(boolVal);
                }
                else
                {
                    // Fallback: deep clone
                    valueToAssign = JsonNode.Parse(computedValue.ToJsonString());
                }
            }
            else
            {
                // For complex types, deep clone
                valueToAssign = JsonNode.Parse(computedValue.ToJsonString());
            }
            
            propertiesObj[propertyKey] = valueToAssign;
        }
        // TODO: Add support for other mutation types (transform, tags, etc.) as needed
    }
}
