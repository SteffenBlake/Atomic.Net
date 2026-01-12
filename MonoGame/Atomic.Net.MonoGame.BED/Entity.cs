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
    public bool Active => EntityRegistry.IsActive(Index);


    /// <summary>
    /// Indicates whether the entity is active and enabled.
    /// </summary>
    public bool Enabled => EntityRegistry.IsEnabled(Index);

    /// <summary>
    /// Sets a behavior on this entity.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="init">Initializer for the behavior.</param>
    public void SetBehavior<TBehavior>(BehavorInit<TBehavior> init)
        where TBehavior : struct
    {
        BehaviorRegistry<TBehavior>.Instance.SetBehavior(this, init);
    }

    /// <summary>
    /// Removes a behavior from this entity.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <returns>True if the behavior was removed.</returns>
    public bool RemoveBehavior<TBehavior>()
        where TBehavior : struct
    {
        return BehaviorRegistry<TBehavior>.Instance.Remove(Index);
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
        return BehaviorRegistry<TBehavior>.Instance.Active[Index];
    }

    /// <summary>
    /// Deactivates this entity.
    /// </summary>
    public void Deactivate() => EntityRegistry.Deactivate(Index);
    
    /// <summary>
    /// Enables this entity.
    /// </summary>
    public void Enable() => EntityRegistry.Enable(Index);

    /// <summary>
    /// Disable this entity.
    /// </summary>
    public void Disable() => EntityRegistry.Disable(Index);
}

