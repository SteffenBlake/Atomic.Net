using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Properties;


// IMPORTANT: Because Properties is a Ref type, it will be assigned a value of 
// null, not [], when you instantiate it via `default` which is EXPECTED BEHAVIOR
//
// Behaviors are IMMUTABLE - Properties is IReadOnlyDictionary to enforce this.
// To "update" a property, create a NEW PropertiesBehavior with a new dictionary.
//
// Example - setting a single property:
//   behavior = new(new Dictionary<string, PropertyValue> { { "key", value } });
//
// Example - preserving existing properties while adding/updating:
//   var dict = new Dictionary<string, PropertyValue>(behavior.Properties ?? []);
//   dict["newKey"] = newValue;
//   behavior = new(dict);
[JsonConverter(typeof(PropertiesBehaviorConverter))]
public readonly record struct PropertiesBehavior(
    IReadOnlyDictionary<string, PropertyValue>? Properties = null
) : IBehavior<PropertiesBehavior>
{
    public static PropertiesBehavior CreateFor(Entity entity)
    {
        return new();
    }
}
