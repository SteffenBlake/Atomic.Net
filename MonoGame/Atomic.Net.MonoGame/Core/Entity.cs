using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Lightweight entity handle.
/// </summary>
/// <param name="index">The entity index.</param>
public readonly struct Entity(PartitionIndex index)
{
    /// <summary>
    /// The entity index.
    /// </summary>
    public readonly PartitionIndex Index = index;

    /// <summary>
    /// Indicates whether the entity is active.
    /// </summary>
    public bool Active => EntityRegistry.Instance.IsActive(this);


    /// <summary>
    /// Indicates whether the entity is active and enabled.
    /// </summary>
    public bool Enabled => EntityRegistry.Instance.IsEnabled(this);

    /// <summary>
    /// Returns true if this entity is in the global partition.
    /// </summary>
    public bool IsGlobal() => Index.IsGlobal;

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

