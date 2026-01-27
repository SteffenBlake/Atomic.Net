using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Tags;

/// <summary>
/// Singleton registry for tracking entity tags and resolving tag-based selectors.
/// Maintains one-to-many mapping (one tag → many entities).
/// </summary>
public sealed class TagRegistry : ISingleton<TagRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<TagsBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<TagsBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<TagsBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<TagsBehavior>>,
    IEventHandler<ResetEvent>,
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
    private readonly Dictionary<string, SparseArray<bool>> _tagToEntities = new(Constants.MaxEntities / 64);
    
    // Entity → tags on that entity (reverse lookup)
    private readonly SparseReferenceArray<ImmutableHashSet<string>> _entityToTags = new(Constants.MaxEntities);
    
    // Tag name → TagEntitySelector (for dirty flag propagation)
    private readonly Dictionary<string, List<TagEntitySelector>> _tagToSelectors = new(Constants.MaxEntities / 64);

    /// <summary>
    /// Registers a tag selector to be marked dirty when the tag is mutated.
    /// Called by SelectorRegistry when creating TagEntitySelector.
    /// </summary>
    public void RegisterSelector(string tag, TagEntitySelector selector)
    {
        var normalizedTag = tag.ToLowerInvariant();
        
        if (!_tagToSelectors.TryGetValue(normalizedTag, out var selectors))
        {
            selectors = new List<TagEntitySelector>();
            _tagToSelectors[normalizedTag] = selectors;
        }
        
        if (!selectors.Contains(selector))
        {
            selectors.Add(selector);
        }
    }

    /// <summary>
    /// Attempts to resolve all entities with the given tag.
    /// Returns true if any entities have this tag, false otherwise.
    /// </summary>
    public bool TryResolve(
        string tag,
        [NotNullWhen(true)]
        out SparseArray<bool>? entities
    )
    {
        var normalizedTag = tag.ToLowerInvariant();
        
        if (_tagToEntities.TryGetValue(normalizedTag, out var entitySet))
        {
            entities = entitySet;
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
        EventBus<ResetEvent>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Clear all registries on shutdown (used between tests)
        _tagToEntities.Clear();
        _entityToTags.Clear();
        _tagToSelectors.Clear();
        
        // Unregister from all events to prevent duplicate registrations
        EventBus<BehaviorAddedEvent<TagsBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<TagsBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<TagsBehavior>>.Unregister(this);
        EventBus<PreBehaviorRemovedEvent<TagsBehavior>>.Unregister(this);
        EventBus<ResetEvent>.Unregister(this);
        EventBus<ShutdownEvent>.Unregister(this);
    }

    public void OnEvent(BehaviorAddedEvent<TagsBehavior> e)
    {
        if (!BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        RegisterTags(e.Entity, behavior.Value.Tags);
    }

    public void OnEvent(PreBehaviorUpdatedEvent<TagsBehavior> e)
    {
        // Unregister old tags before update
        if (_entityToTags.TryGetValue(e.Entity.Index, out var oldTags))
        {
            UnregisterTags(e.Entity, oldTags);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<TagsBehavior> e)
    {
        // Register new tags after update
        if (!BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        RegisterTags(e.Entity, behavior.Value.Tags);
    }

    public void OnEvent(PreBehaviorRemovedEvent<TagsBehavior> e)
    {
        // Unregister all tags when behavior is removed
        if (_entityToTags.TryGetValue(e.Entity.Index, out var tags))
        {
            UnregisterTags(e.Entity, tags);
        }
    }

    public void OnEvent(ResetEvent _)
    {
        // senior-dev: Clear scene partition tags only (indices >= MaxGlobalEntities)
        // Global partition tags persist through ResetEvent
        
        // Iterate through all tags and remove scene entities
        foreach (var (tag, entitySet) in _tagToEntities)
        {
            for (ushort i = Constants.MaxGlobalEntities; i < Constants.MaxEntities; i++)
            {
                if (entitySet.HasValue(i))
                {
                    entitySet.Remove(i);
                }
            }
        }
        
        // Clear entity-to-tags reverse lookup for scene entities
        for (ushort i = Constants.MaxGlobalEntities; i < Constants.MaxEntities; i++)
        {
            _entityToTags.Remove(i);
        }
    }

    private void RegisterTags(Entity entity, ImmutableHashSet<string> tags)
    {
        // Store entity-to-tags reverse lookup
        _entityToTags[entity.Index] = tags;
        
        foreach (var tag in tags)
        {
            // Ensure tag-to-entities set exists
            if (!_tagToEntities.TryGetValue(tag, out var entitySet))
            {
                entitySet = new SparseArray<bool>(Constants.MaxEntities);
                _tagToEntities[tag] = entitySet;
            }
            
            // Add entity to tag set
            entitySet.Set(entity.Index, true);
            
            // Mark all selectors for this tag as dirty
            if (_tagToSelectors.TryGetValue(tag, out var selectors))
            {
                foreach (var selector in selectors)
                {
                    selector.MarkDirty();
                }
            }
        }
    }

    private void UnregisterTags(Entity entity, ImmutableHashSet<string> tags)
    {
        // Remove entity-to-tags reverse lookup
        _entityToTags.Remove(entity.Index);
        
        foreach (var tag in tags)
        {
            // Remove entity from tag set
            if (_tagToEntities.TryGetValue(tag, out var entitySet))
            {
                entitySet.Remove(entity.Index);
            }
            
            // Mark all selectors for this tag as dirty
            if (_tagToSelectors.TryGetValue(tag, out var selectors))
            {
                foreach (var selector in selectors)
                {
                    selector.MarkDirty();
                }
            }
        }
    }
}
