using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Sequencing;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes SceneCommands by delegating to appropriate command drivers.
/// Shared between RulesDriver and SequenceDriver to avoid code duplication.
/// </summary>
public static class SceneCommandDriver
{
    /// <summary>
    /// Executes a SceneCommand on an entity with the given context.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="context">The context JsonNode for JsonLogic evaluation</param>
    /// <param name="entityJson">The entity JsonNode to mutate</param>
    /// <param name="entityIndex">The entity index (for sequence commands)</param>
    public static void Execute(
        SceneCommand command,
        JsonNode context,
        JsonNode entityJson,
        ushort entityIndex
    )
    {
        if (command.TryMatch(out MutCommand mutCommand))
        {
            MutCmdDriver.Execute(mutCommand, context, entityJson);
        }
        else if (command.TryMatch(out SequenceStartCommand startCmd))
        {
            SequenceStartCmdDriver.Execute(entityIndex, startCmd.SequenceId);
        }
        else if (command.TryMatch(out SequenceStopCommand stopCmd))
        {
            SequenceStopCmdDriver.Execute(entityIndex, stopCmd.SequenceId);
        }
        else if (command.TryMatch(out SequenceResetCommand resetCmd))
        {
            SequenceResetCmdDriver.Execute(entityIndex, resetCmd.SequenceId);
        }
    }
}
