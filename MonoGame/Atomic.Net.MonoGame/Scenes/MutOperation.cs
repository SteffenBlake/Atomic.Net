using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents a single mutation operation in a rule's 'mut' array.
/// Each operation specifies a target path and a value expression.
/// </summary>
[JsonConverter(typeof(MutOperationConverter))]
public readonly record struct MutOperation(
    /// <summary>
    /// Path object specifying where to mutate (e.g., { "properties": "health" })
    /// </summary>
    JsonNode Target,
    
    /// <summary>
    /// JsonLogic expression to evaluate and set at the target path
    /// </summary>
    JsonNode Value
);
