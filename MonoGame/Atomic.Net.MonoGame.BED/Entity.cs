using System.Diagnostics.CodeAnalysis;
namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Lightweight entity handle.
/// </summary>
/// <param name="index">The entity index.</param>
public readonly struct Entity(ushort index)
{
    /// <summary>
    /// The entity index.
    /// </summary>
    public readonly ushort Index = index;

    /// <summary>
    /// Indicates whether the entity is active.
    /// </summary>
    public bool Active => EntityRegistry.Instance.IsActive(this);


    /// <summary>
    /// Indicates whether the entity is active and enabled.
    /// </summary>
    public bool Enabled => EntityRegistry.Instance.IsEnabled(this);

    /// <summary>
    /// Sets a behavior on this entity.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="mutate">Action to mutate the behavior by reference.</param>
    public void SetBehavior<TBehavior>(RefAction<TBehavior> mutate)
        where TBehavior : struct
    {
        BehaviorRegistry<TBehavior>.Instance.SetBehavior(this, mutate);
    }

    /// <summary>
    /// Removes a behavior from this entity.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <returns>True if the behavior was removed.</returns>
    public bool RemoveBehavior<TBehavior>()
        where TBehavior : struct
    {
        return BehaviorRegistry<TBehavior>.Instance.Remove(this);
    }

    /// <summary>
    /// Attempts to retrieve a behavior from this entity.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="behavior">The behavior instance.</param>
    /// <returns>True if the behavior exists.</returns>
    public bool TryGetBehavior<TBehavior>(
        [NotNullWhen(true)] out TBehavior? behavior
    )
        where TBehavior : struct
    {
        return BehaviorRegistry<TBehavior>.Instance.TryGetBehavior(this, out behavior);
    }

    /// <summary>
    /// Checks whether this entity has a specific behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <returns>True if the behavior is active.</returns>
    public bool HasBehavior<TBehavior>()
        where TBehavior : struct
    {
        return BehaviorRegistry<TBehavior>.Instance.HasBehavior(this);
    }

    /// <summary>
    /// Deactivates this entity.
    /// </summary>
    public void Deactivate() => EntityRegistry.Instance.Deactivate(this);
    
    /// <summary>
    /// Enables this entity.
    /// </summary>
    public void Enable() => EntityRegistry.Instance.Enable(this);

    /// <summary>
    /// Disable this entity.
    /// </summary>
    public void Disable() => EntityRegistry.Instance.Disable(this);
}

