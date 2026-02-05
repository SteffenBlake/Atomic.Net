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
    IEventHandler<PreBehaviorRemovedEvent<TagsBehavior>>,
    IEventHandler<ShutdownEvent>
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
    private readonly Dictionary<string, PartitionedSparseArray<bool>> _tagToEntities = new();

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
            return true;
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
        EventBus<ShutdownEvent>.Register(this);
    }

    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: FINDING: Unregistering from PreBehaviorRemovedEvent in ShutdownEvent
        // creates a potential race condition. EntityRegistry.OnEvent(ShutdownEvent) deactivates 
        // entities, which should trigger PreBehaviorRemovedEvent for cleanup. However, if we
        // unregister here before the cascade completes, we miss cleanup events.
        // 
        // This bug exists in EntityIdRegistry and PropertiesRegistry too. The initialization 
        // order in AtomicSystem.Initialize() may affect whether this manifests - EntityRegistry
        // initializes before TagRegistry, so EntityRegistry.OnEvent(ShutdownEvent) should run
        // first and trigger the cleanup cascade before we unregister.
        //
        // Workaround: Manually clear _tagToEntities until proper fix is implemented.
        // Proper fix would be: Don't unregister in ShutdownEvent, only in destructor/dispose.
        _tagToEntities.Clear();

        // Unregister from all events to prevent duplicate registrations
        EventBus<BehaviorAddedEvent<TagsBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<TagsBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<TagsBehavior>>.Unregister(this);
        EventBus<PreBehaviorRemovedEvent<TagsBehavior>>.Unregister(this);
        EventBus<ShutdownEvent>.Unregister(this);
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
            }
        }
    }
}
