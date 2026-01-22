using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Properties;

[JsonConverter(typeof(PropertiesBehaviorConverter))]
public readonly record struct PropertiesBehavior(
    Dictionary<string, PropertiesBehavior> Value
) :IBehavior<PropertiesBehavior>
{
    public static PropertiesBehavior CreateFor(Entity entity)
    {
        return new([]);
    }
}

