using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Properties;


// IMPORTANT: Because Properties is a Ref type, it will be assigned a value of 
// null, not [], when you instantiate it via `default` which is EXPECTED BEHAVIOR
//
// DO NOT constantly `new` it up, it should be immutable as a behavior.
//
// Instead, just always defensively null check Properties before accessing it
[JsonConverter(typeof(PropertiesBehaviorConverter))]
public readonly record struct PropertiesBehavior(
    Dictionary<string, PropertyValue>? Properties = null
) : IBehavior<PropertiesBehavior>
{

    public static PropertiesBehavior CreateFor(Entity entity)
    {
        return new();
    }
}

