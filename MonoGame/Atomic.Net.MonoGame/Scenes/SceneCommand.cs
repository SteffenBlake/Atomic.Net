using System.Text.Json.Serialization;
using dotVariant;

namespace Atomic.Net.MonoGame.Scenes;

[Variant]
[JsonConverter(typeof(SceneCommandConverter))]
public readonly partial struct SceneCommand
{
    static partial void VariantOf(MutCommand mut);
}

