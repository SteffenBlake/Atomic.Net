using System.Collections.Immutable;
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
    // senior-dev: ImmutableHashSet allocation is approved by SteffenBlake (sprint file line 179)
    // This is a load-time allocation, not a gameplay allocation
    private readonly ImmutableHashSet<string>? _tags;
    
    public ImmutableHashSet<string> Tags
    {
        init => _tags = value;
        get => _tags ?? ImmutableHashSet<string>.Empty;
    }
    
    public static TagsBehavior CreateFor(Entity entity)
    {
        return new TagsBehavior();
    }
}
