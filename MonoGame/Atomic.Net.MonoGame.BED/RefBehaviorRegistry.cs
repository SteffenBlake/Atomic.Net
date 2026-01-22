using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Central registry for storing and managing backed behaviors per entity.
/// Backed behaviors implement IBehavior and are auto-created.
/// </summary>
/// <typeparam name="TBehavior">The backed behavior type.</typeparam>
public class RefBehaviorRegistry<TBehavior> : 
    ISingleton<RefBehaviorRegistry<TBehavior>>, 
    IEventHandler<InitializeEvent>,
    IEventHandler<PreEntityDeactivatedEvent>
    where TBehavior : struct, IBehavior<TBehavior>
{
    public static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register<RefBehaviorRegistry<TBehavior>>();
    }

    public static RefBehaviorRegistry<TBehavior> Instance { get; private set; } = null!;

    /// <summary>
    /// Handles initialization by registering for entity deactivation events.
    /// </summary>
    public void OnEvent(InitializeEvent _)
    {
        EventBus<PreEntityDeactivatedEvent>.Register(this);
    }

    private readonly SparseArray<TBehavior> _behaviors = new(Constants.MaxEntities);

    /// <summary>
    /// Sets a backed behavior. Auto-creates via IBehavior.CreateFor if missing.
    /// Cannot reassign the behavior itself, only modify backing values.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <param name="mutate">Action to mutate the behavior's backing values.</param>
    public void SetBehavior(Entity entity, RefReadonlyAction<TBehavior> mutate)
    {
        if (!entity.Active)
        {
            throw new InvalidOperationException(
                $"Attempted to set behavior '{typeof(TBehavior)}' for inactive entity with id: {entity.Index}."
            );
        }

        var isNew = !_behaviors.HasValue(entity.Index);

        if (!isNew)
        {
            EventBus<PreBehaviorUpdatedEvent<TBehavior>>.Push(new(entity));
        }

        using var behaviorRef = _behaviors.GetMut(entity.Index);
        if (isNew)
        {
            behaviorRef.Value = TBehavior.CreateFor(entity);
        }
        mutate(ref behaviorRef.Value);

        // senior-dev: Invoke global mutation callback if registered (used by DatabaseRegistry)
        BehaviorMutationTracker.OnEntityMutated?.Invoke(entity.Index);

        if (isNew)
        {
            EventBus<BehaviorAddedEvent<TBehavior>>.Push(new(entity));
        }
        else
        {
            EventBus<PostBehaviorUpdatedEvent<TBehavior>>.Push(new(entity));
        }
    }

    /// <summary>
    /// Removes (deactivates) a behavior for the given entity.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <returns>True if the behavior was removed.</returns>
    public bool Remove(Entity entity)
    {
        var removed = _behaviors.Remove(entity.Index);
        if (removed)
        {
            EventBus<PreBehaviorRemovedEvent<TBehavior>>.Push(new(entity));
        }
        return removed;
    }

    /// <summary>
    /// Gets a read-only reference to the behavior for the given entity.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <param name="behavior">The behavior value (if present).</param>
    /// <returns>True if the behavior is present.</returns>
    public bool TryGetBehavior(Entity entity, [NotNullWhen(true)] out TBehavior? behavior)
    {
        return _behaviors.TryGetValue(entity.Index, out behavior);
    }

    /// <summary>
    /// Checks if the behavior is present for the given entity.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <returns>True if the behavior is present.</returns>
    public bool HasBehavior(Entity entity)
    {
        return _behaviors.HasValue(entity.Index);
    }

    /// <summary>
    /// Handles entity deactivation by cleaning up associated behaviors.
    /// </summary>
    /// <param name="evt">The deactivation event.</param>
    public void OnEvent(PreEntityDeactivatedEvent evt)
    {
        Remove(evt.Entity);
    }
}
