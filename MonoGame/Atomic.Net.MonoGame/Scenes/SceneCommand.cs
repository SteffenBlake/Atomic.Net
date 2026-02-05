using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core.Extensions;
using Atomic.Net.MonoGame.Sequencing;
using dotVariant;

namespace Atomic.Net.MonoGame.Scenes;

[Variant]
[JsonConverter(typeof(SceneCommandConverter))]
public readonly partial struct SceneCommand
{
    static partial void VariantOf(
        MutCommand mut,
        SequenceStartCommand sequenceStart,
        SequenceStopCommand sequenceStop,
        SequenceResetCommand sequenceReset,
        SceneLoadCommand sceneLoad
    );

    /// <summary>
    /// Executes the command on the given JsonNode entity with the provided context.
    /// Dispatches to the appropriate command implementation based on variant type.
    /// </summary>
    /// <param name="jsonEntity">The JSON entity to mutate</param>
    /// <param name="context">The JsonLogic context containing world, entities, and self</param>
    public void Execute(JsonNode jsonEntity, JsonObject context)
    {
        if (TryMatch(out MutCommand mutCommand))
        {
            mutCommand.Execute(jsonEntity, context);
        }
        else if (TryMatch(out SequenceStartCommand startCommand))
        {
            if (!jsonEntity.TryGetSceneEntityIndex(out var entityIndex))
            {
                return;
            }
            SequenceStartCmdDriver.Instance.Execute(entityIndex.Value, startCommand.SequenceId);
        }
        else if (TryMatch(out SequenceStopCommand stopCommand))
        {
            if (!jsonEntity.TryGetSceneEntityIndex(out var entityIndex))
            {
                return;
            }
            SequenceStopCmdDriver.Instance.Execute(entityIndex.Value, stopCommand.SequenceId);
        }
        else if (TryMatch(out SequenceResetCommand resetCommand))
        {
            if (!jsonEntity.TryGetSceneEntityIndex(out var entityIndex))
            {
                return;
            }
            SequenceResetCmdDriver.Instance.Execute(entityIndex.Value, resetCommand.SequenceId);
        }
        else if (TryMatch(out SceneLoadCommand loadCommand))
        {
            // Scene load is global operation, no entity context needed
            SceneLoadCmdDriver.Instance.Execute(loadCommand.ScenePath);
        }
    }
}

