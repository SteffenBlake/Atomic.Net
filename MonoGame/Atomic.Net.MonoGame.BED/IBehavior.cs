namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Interface for behaviors that can be auto-created for entities.
/// </summary>
/// <typeparam name="TSelf">The implementing behavior type.</typeparam>
public interface IBehavior<TSelf>
    where TSelf : struct, IBehavior<TSelf>
{
    /// <summary>
    /// Creates a behavior instance bound to the specified entity index.
    /// </summary>
    static abstract TSelf CreateFor(int entityIndex);
}
