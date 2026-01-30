using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Scenes.JsonTargets;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents a single mutation operation in a rule's 'mut' array.
/// Each operation specifies a target path and a value expression.
/// </summary>
[JsonConverter(typeof(MutOperationConverter))]
public readonly record struct MutOperation(
    /// <summary>
    /// Strongly-typed target specifying where to mutate.
    /// Deserialized at scene load time using JsonTargetConverter.
    /// </summary>
    JsonTarget Target,
    
    /// <summary>
    /// JsonLogic expression to evaluate and set at the target path
    /// </summary>
    JsonNode Value
);
