using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes MutCommand operations by mutating entity JsonNodes in-place.
/// Does NOT write changes back to actual Entity behaviors - that's handled by JsonEntityConverter.Write.
/// </summary>
public static class MutCmdDriver
{
    public static void Execute(MutCommand command, JsonNode context, JsonNode entityJson)
    {
        foreach (var operation in command.Operations)
        {
            ApplyOperation(operation, context, entityJson);
        }
    }

    private static void ApplyOperation(MutOperation operation, JsonNode context, JsonNode entityJson)
    {
        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity JSON is not a JsonObject"));
            return;
        }

        if (!TryApplyJsonLogic(operation.Value, context, out var computedValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Failed to evaluate mutation value"));
            return;
        }

        // Clone the computed value before assignment
        // Per DISCOVERIES.md: JsonLogic.Apply() returns the original node reference for literals,
        // which still has a parent (the deserialized logic tree). We can't remove from parent
        // because the logic tree needs to be reused. DeepClone() is necessary here.
        var clonedValue = computedValue.DeepClone();

        // Target must be a nested JsonObject representing the path to the target field
        // Examples: {"properties": "health"}, {"transform": {"position": "x"}}, {"id": true}
        if (operation.Target is not JsonObject targetObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Target must be a JsonObject specifying the path"));
            return;
        }

        // Navigate the target path and set the value
        SetValueAtPath(entityObj, targetObj, clonedValue);
    }

    private static void SetValueAtPath(JsonObject entityJson, JsonObject targetPath, JsonNode value)
    {
        // The target path is a nested JsonObject that mirrors the entity JSON structure
        // Examples:
        // {"properties": "health"} -> entityJson["properties"]["health"] = value
        // {"transform": {"position": "x"}} -> entityJson["transform"]["position"]["x"] = value
        // {"id": true} -> entityJson["id"] = value (the value in target doesn't matter for simple fields)
        // {"flexGrow": true} -> entityJson["flexGrow"] = value
        
        if (targetPath.Count != 1)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Target path must have exactly one root key, got {targetPath.Count}"));
            return;
        }

        var rootKey = targetPath.First().Key;
        var rootValue = targetPath.First().Value;

        // Navigate to the target location
        if (rootValue is JsonObject nestedPath)
        {
            // Multi-level path like {"transform": {"position": "x"}}
            // Ensure the intermediate object exists
            if (!entityJson.TryGetPropertyValue(rootKey, out var intermediate) || intermediate is not JsonObject intermediateObj)
            {
                // Create intermediate object if it doesn't exist
                intermediateObj = new JsonObject();
                entityJson[rootKey] = intermediateObj;
            }

            // Recursively navigate deeper
            SetValueAtPath((JsonObject)intermediateObj, nestedPath, value);
        }
        else if (rootValue is JsonValue leafValue)
        {
            // We're at a leaf in the path
            // The value could be:
            // 1. A string specifying the final key (e.g., "health" in {"properties": "health"})
            // 2. A boolean/other value that's just a marker (e.g., true in {"id": true})
            
            if (leafValue.TryGetValue<string>(out var finalKey))
            {
                // Case 1: The leaf value is a string key (e.g., {"properties": "health"})
                // Ensure the parent object exists
                if (!entityJson.TryGetPropertyValue(rootKey, out var parent) || parent is not JsonObject parentObj)
                {
                    // Create parent object if it doesn't exist
                    parentObj = new JsonObject();
                    entityJson[rootKey] = parentObj;
                }

                // Set the value at the final key
                parentObj[finalKey] = value;
            }
            else
            {
                // Case 2: The leaf value is just a marker (e.g., {"id": true})
                // Set the value directly at the root key
                entityJson[rootKey] = value;
            }
        }
        else
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Invalid target path structure at key '{rootKey}'"));
        }
    }

    private static bool TryApplyJsonLogic(JsonNode logic, JsonNode data, [NotNullWhen(true)] out JsonNode? result)
    {
        try
        {
            result = JsonLogic.Apply(logic, data);
            return result != null;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"JsonLogic evaluation failed: {ex.Message}"));
            result = null;
            return false;
        }
    }
}
