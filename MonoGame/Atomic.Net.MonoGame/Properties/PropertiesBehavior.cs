using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Properties;

[JsonConverter(typeof(PropertiesBehaviorConverter))]
public readonly struct PropertiesBehavior() : IBehavior<PropertiesBehavior>
{
    public readonly Dictionary<string, PropertyValue> Properties { get; } = [];

    public static PropertiesBehavior CreateFor(Entity entity)
    {
        return new();
    }
}

