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

        // Target must be a string representing the path to a leaf value
        if (operation.Target is not JsonValue targetValue || !targetValue.TryGetValue<string>(out var targetPath))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Target must be a string path to a leaf value"));
            return;
        }

        // Parse the path and navigate to set the leaf value
        SetLeafValue(entityObj, targetPath, clonedValue);
    }

    private static void SetLeafValue(JsonObject entityJson, string path, JsonNode value)
    {
        var parts = path.Split('.');
        
        if (parts.Length == 1)
        {
            // Simple field like "id", "flexGrow", "parent"
            entityJson[parts[0]] = value;
            return;
        }

        // Navigate to the parent of the leaf
        JsonObject? current = entityJson;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            
            if (!current.TryGetPropertyValue(part, out var node) || node is not JsonObject obj)
            {
                // Create the intermediate object if it doesn't exist
                obj = new JsonObject();
                current[part] = obj;
            }
            
            current = (JsonObject)obj;
        }

        // Set the leaf value
        current[parts[^1]] = value;
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
