using System.Text.Json.Serialization;
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
        SequenceResetCommand sequenceReset
    );
}

