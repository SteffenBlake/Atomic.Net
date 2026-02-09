using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tags;

/// <summary>
/// Singleton registry for tracking entity tags and resolving tag-based queries.
/// Maintains one-to-many mapping (one tag → many entities).
/// </summary>
public sealed class TagRegistry : ISingleton<TagRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<TagsBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<TagsBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<TagsBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<TagsBehavior>>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    public static TagRegistry Instance { get; private set; } = null!;

    // Tag name → entities with that tag (one-to-many)
    private readonly Dictionary<string, PartitionedSparseArray<bool>> _tagToEntities = [];

    /// <summary>
    /// Attempts to resolve all entities with the given tag.
    /// Returns true if any entities have this tag, false otherwise.
    /// </summary>
    public bool TryResolve(
        string tag,
        [NotNullWhen(true)]
        out PartitionedSparseArray<bool>? entities
    )
    {
        var normalizedTag = tag.ToLower();

        if (_tagToEntities.TryGetValue(normalizedTag, out entities))
        {
            // Only return true if there are actually entities with this tag
            if (entities.Global.Count > 0 || entities.Scene.Count > 0)
            {
                return true;
            }
        }

        entities = null;
        return false;
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<TagsBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<TagsBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<TagsBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<TagsBehavior>>.Register(this);
    }

    public void OnEvent(BehaviorAddedEvent<TagsBehavior> e)
    {
        if (!e.Entity.TryGetBehavior<TagsBehavior>(out var behavior))
        {
            return;
        }

        RegisterTags(e.Entity, behavior.Value.Tags);
    }

    public void OnEvent(PreBehaviorUpdatedEvent<TagsBehavior> e)
    {
        // Unregister old tags before update
        if (e.Entity.TryGetBehavior<TagsBehavior>(out var behavior))
        {
            UnregisterTags(e.Entity, behavior.Value.Tags);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<TagsBehavior> e)
    {
        // Register new tags after update
        if (!e.Entity.TryGetBehavior<TagsBehavior>(out var behavior))
        {
            return;
        }

        RegisterTags(e.Entity, behavior.Value.Tags);
    }

    public void OnEvent(PreBehaviorRemovedEvent<TagsBehavior> e)
    {
        // Unregister all tags when behavior is removed
        if (e.Entity.TryGetBehavior<TagsBehavior>(out var behavior))
        {
            UnregisterTags(e.Entity, behavior.Value.Tags);
        }
    }

    private void RegisterTags(Entity entity, FluentHashSet<string> tags)
    {
        foreach (var tag in tags)
        {
            // Ensure tag-to-entities set exists
            if (!_tagToEntities.TryGetValue(tag, out var entitySet))
            {
                entitySet = new PartitionedSparseArray<bool>(
                    Constants.MaxGlobalEntities,
                    Constants.MaxSceneEntities
                );
                _tagToEntities[tag] = entitySet;
            }

            // Add entity to tag set
            entitySet.Set(entity.Index, true);
        }
    }

    private void UnregisterTags(Entity entity, FluentHashSet<string> tags)
    {
        foreach (var tag in tags)
        {
            // Remove entity from tag set
            if (_tagToEntities.TryGetValue(tag, out var entitySet))
            {
                entitySet.Remove(entity.Index);

                // Clean up empty tag sets to prevent dictionary bloat
                if (entitySet.Global.Count == 0 && entitySet.Scene.Count == 0)
                {
                    _tagToEntities.Remove(tag);
                }
            }
        }
    }
}
