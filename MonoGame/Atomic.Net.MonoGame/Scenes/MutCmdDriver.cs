using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Driver for executing mutation commands on entities.
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

            MutationHelper.ApplyToTarget(entityIndex, operation.Target, computedValue);
        }
    }
}
