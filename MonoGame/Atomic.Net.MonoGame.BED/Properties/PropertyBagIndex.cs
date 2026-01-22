using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Properties;

/// <summary>
/// Singleton index for fast property-based entity queries.
/// Maintains inverted indices for key-only and key-value lookups.
/// </summary>
public sealed class PropertyBagIndex : ISingleton<PropertyBagIndex>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<PropertiesBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<PropertiesBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PropertiesBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PropertiesBehavior>>
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

    public static PropertyBagIndex Instance { get; private set; } = null!;

    // senior-dev: Key-only index: key → entities with that key (case-insensitive)
    private readonly Dictionary<string, SparseArray<bool>> _keyIndex = 
        new(StringComparer.OrdinalIgnoreCase);

    // senior-dev: Key-value index: key → value → entities (outer key is case-insensitive)
    private readonly Dictionary<string, Dictionary<PropertyValue, SparseArray<bool>>> _keyValueIndex = 
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Indexes all properties for an entity.
    /// </summary>
    public void IndexProperties(Entity entity, Dictionary<string, PropertyValue> properties)
    {
        foreach (var (key, value) in properties)
        {
            // senior-dev: Add to key-only index
            if (!_keyIndex.TryGetValue(key, out var keyEntities))
            {
                keyEntities = new SparseArray<bool>(Constants.MaxEntities);
                _keyIndex[key] = keyEntities;
            }
            keyEntities.Set(entity.Index, true);

            // senior-dev: Add to key-value index
            if (!_keyValueIndex.TryGetValue(key, out var valueDict))
            {
                valueDict = new Dictionary<PropertyValue, SparseArray<bool>>();
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
    public void RemoveProperties(Entity entity, Dictionary<string, PropertyValue> properties)
    {
        foreach (var (key, value) in properties)
        {
            // senior-dev: Remove from key-only index
            if (_keyIndex.TryGetValue(key, out var keyEntities))
            {
                keyEntities.Remove(entity.Index);
            }

            // senior-dev: Remove from key-value index
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
            yield break;
        }

        // senior-dev: Zero-alloc iteration over SparseArray (returns struct enumerator)
        foreach (var (entityIndex, hasKey) in keyEntities)
        {
            if (hasKey)
            {
                yield return EntityRegistry.Instance[entityIndex];
            }
        }
    }

    /// <summary>
    /// Gets all entities that have a property with the given key and value (case-insensitive key).
    /// </summary>
    public IEnumerable<Entity> GetEntitiesWithKeyValue(string key, PropertyValue value)
    {
        if (!_keyValueIndex.TryGetValue(key, out var valueDict))
        {
            yield break;
        }

        if (!valueDict.TryGetValue(value, out var valueEntities))
        {
            yield break;
        }

        // senior-dev: Zero-alloc iteration over SparseArray (returns struct enumerator)
        foreach (var (entityIndex, hasValue) in valueEntities)
        {
            if (hasValue)
            {
                yield return EntityRegistry.Instance[entityIndex];
            }
        }
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<PropertiesBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<PropertiesBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PropertiesBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<PropertiesBehavior>>.Register(this);
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
