using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using dotVariant;

namespace Atomic.Net.MonoGame.Scenes;

[Variant]
[JsonConverter(typeof(SceneCommandConverter))]
public readonly partial struct SceneCommand
{
    static partial void VariantOf(MutCommand mut);

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
        // Future: Add other command types here
        // else if (TryMatch(out SomeOtherCommand otherCommand))
        // {
        //     otherCommand.Execute(jsonEntity, context);
        // }
    }
}

