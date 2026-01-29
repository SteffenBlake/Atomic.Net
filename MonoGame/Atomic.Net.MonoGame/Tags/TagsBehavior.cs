using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tags;

/// <summary>
/// Behavior that tracks an entity's tags for group selection in rules and queries.
/// Tags are case-insensitive string labels (e.g., "enemy", "boss", "flying").
/// </summary>
[JsonConverter(typeof(TagsBehaviorConverter))]
public readonly record struct TagsBehavior : IBehavior<TagsBehavior>
{
    // senior-dev: FluentHashSet allocation is approved (following PropertiesBehavior pattern)
    // This is a load-time allocation, not a gameplay allocation
    private readonly FluentHashSet<string>? _tags;
    
    public FluentHashSet<string> Tags
    {
        init => _tags = value;
        get => _tags ?? new(8, StringComparer.OrdinalIgnoreCase);
    }
    
    public static TagsBehavior CreateFor(Entity entity)
    {
        return new TagsBehavior();
    }
}
