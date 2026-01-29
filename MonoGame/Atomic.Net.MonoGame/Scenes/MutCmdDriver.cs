using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Driver for executing mutation commands on entities.
/// Delegates to RulesDriver's mutation logic.
/// </summary>
public static class MutCmdDriver
{
    /// <summary>
    /// Executes a MutCommand on an entity with the provided context.
    /// </summary>
    /// <param name="command">The mutation command to execute.</param>
    /// <param name="context">JsonNode context for evaluating mutation values.</param>
    /// <param name="entityIndex">The index of the entity to mutate.</param>
    public static void Execute(MutCommand command, JsonNode context, ushort entityIndex)
    {
        RulesDriver.ExecuteMutations(command, context, entityIndex);
    }
}
