using System.Collections.Immutable;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Properties;

/// <summary>
/// Singleton index for fast property-based entity queries.
/// Maintains inverted indices for key-only and key-value lookups.
/// </summary>
public sealed class PropertiesRegistry : ISingleton<PropertiesRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<PropertiesBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<PropertiesBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PropertiesBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PropertiesBehavior>>,
    IEventHandler<ShutdownEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    public static PropertiesRegistry Instance { get; private set; } = null!;

    private readonly Dictionary<string, SparseArray<bool>> _keyIndex = 
        new(Constants.DefaultAllocPropertyBag, StringComparer.InvariantCultureIgnoreCase);

    private readonly Dictionary<string, Dictionary<PropertyValue, SparseArray<bool>>> _keyValueIndex = 
        new(Constants.DefaultAllocPropertyBag, StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Indexes all properties for an entity.
    /// </summary>
    public void IndexProperties(Entity entity, ImmutableDictionary<string, PropertyValue> properties)
    {
        // senior-dev: No null check needed - ImmutableDictionary never returns null from getter
        foreach (var (key, value) in properties)
        {
            if (!_keyIndex.TryGetValue(key, out var keyEntities))
            {
                keyEntities = new SparseArray<bool>(Constants.MaxEntities);
                _keyIndex[key] = keyEntities;
            }
            keyEntities.Set(entity.Index, true);

            if (!_keyValueIndex.TryGetValue(key, out var valueDict))
            {
                valueDict = [];
                _keyValueIndex[key] = valueDict;
            }

            if (!valueDict.TryGetValue(value, out var valueEntities))
            {
                valueEntities = new SparseArray<bool>(Constants.MaxEntities);
                valueDict[value] = valueEntities;
            }
            valueEntities.Set(entity.Index, true);
        }
    }

    /// <summary>
    /// Removes all indexed properties for an entity.
    /// </summary>
    public void RemoveProperties(Entity entity, ImmutableDictionary<string, PropertyValue> properties)
    {
        // senior-dev: No null check needed - ImmutableDictionary never returns null from getter
        foreach (var (key, value) in properties)
        {
            if (_keyIndex.TryGetValue(key, out var keyEntities))
            {
                keyEntities.Remove(entity.Index);
            }

            if (_keyValueIndex.TryGetValue(key, out var valueDict))
            {
                if (valueDict.TryGetValue(value, out var valueEntities))
                {
                    valueEntities.Remove(entity.Index);
                }
            }
        }
    }

    /// <summary>
    /// Gets all entities that have a property with the given key (case-insensitive).
    /// </summary>
    public IEnumerable<Entity> GetEntitiesWithKey(string key)
    {
        if (!_keyIndex.TryGetValue(key, out var keyEntities))
        {
            return [];
        }

        return keyEntities.Select(static e => EntityRegistry.Instance[e.Index]);
    }

    /// <summary>
    /// Gets all entities that have a property with the given key and value (case-insensitive key).
    /// </summary>
    public IEnumerable<Entity> GetEntitiesWithKeyValue(string key, PropertyValue value)
    {
        if (!_keyValueIndex.TryGetValue(key, out var valueDict))
        {
            return [];
        }

        if (!valueDict.TryGetValue(value, out var valueEntities))
        {
            return [];
        }

        return valueEntities.Select(static e => EntityRegistry.Instance[e.Index]);
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<PropertiesBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<PropertiesBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PropertiesBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<PropertiesBehavior>>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Unregister from all events to prevent duplicate registrations
        EventBus<BehaviorAddedEvent<PropertiesBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<PropertiesBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<PropertiesBehavior>>.Unregister(this);
        EventBus<PreBehaviorRemovedEvent<PropertiesBehavior>>.Unregister(this);
        EventBus<ShutdownEvent>.Unregister(this);
    }

    public void OnEvent(BehaviorAddedEvent<PropertiesBehavior> e)
    {
        if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e.Entity, out var propertiesBehavior))
        {
            IndexProperties(e.Entity, propertiesBehavior.Value.Properties);
        }
    }

    public void OnEvent(PreBehaviorUpdatedEvent<PropertiesBehavior> e)
    {
        if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e.Entity, out var propertiesBehavior))
        {
            RemoveProperties(e.Entity, propertiesBehavior.Value.Properties);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PropertiesBehavior> e)
    {
        if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e.Entity, out var propertiesBehavior))
        {
            IndexProperties(e.Entity, propertiesBehavior.Value.Properties);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PropertiesBehavior> e)
    {
        if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e.Entity, out var propertiesBehavior))
        {
            RemoveProperties(e.Entity, propertiesBehavior.Value.Properties);
        }
    }
}
