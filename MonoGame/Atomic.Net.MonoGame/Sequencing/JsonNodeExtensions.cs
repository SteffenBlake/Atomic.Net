using System.Text.Json.Nodes;
using Json.Logic;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Extension methods for JsonNode to support sequence operations.
/// </summary>
public static class JsonNodeExtensions
{
    /// <summary>
    /// Evaluates a JsonLogic condition with the given context and returns the boolean result.
    /// </summary>
    /// <param name="condition">The JsonLogic condition to evaluate</param>
    /// <param name="context">The context containing variables for evaluation</param>
    /// <param name="result">The boolean result of the condition</param>
    /// <returns>True if the condition evaluated successfully to a boolean, false otherwise</returns>
    public static bool TryEvaluateCondition(
        this JsonNode condition,
        JsonNode context,
        out bool result
    )
    {
        var evalResult = JsonLogic.Apply(condition, context);
        if (evalResult != null && evalResult is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var boolValue))
        {
            result = boolValue;
            return true;
        }

        result = false;
        return false;
    }
}
