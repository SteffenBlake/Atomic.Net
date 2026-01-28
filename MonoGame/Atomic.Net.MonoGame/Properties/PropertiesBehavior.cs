using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Properties;

/// <summary>
/// Behavior that stores arbitrary key-value properties for an entity.
/// Properties are stored as an immutable dictionary with case-insensitive keys.
/// </summary>
[JsonConverter(typeof(PropertiesBehaviorConverter))]
public readonly record struct PropertiesBehavior : IBehavior<PropertiesBehavior>
{
    // senior-dev: ImmutableDictionary allocation is approved (following TagsBehavior pattern)
    // This is a load-time allocation, not a gameplay allocation
    private readonly ImmutableDictionary<string, PropertyValue>? _properties;
    
    public ImmutableDictionary<string, PropertyValue> Properties
    {
        init => _properties = value;
        get => _properties ?? ImmutableDictionary<string, PropertyValue>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
    }
    
    public static PropertiesBehavior CreateFor(Entity entity)
    {
        return new PropertiesBehavior();
    }
}
