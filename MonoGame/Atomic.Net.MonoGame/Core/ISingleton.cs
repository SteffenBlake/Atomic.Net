namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Defines a strongly-typed singleton contract using static abstract members.
/// </summary>
/// <typeparam name="TSelf">The concrete singleton type.</typeparam>
public interface ISingleton<TSelf>
    where TSelf : ISingleton<TSelf>
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static abstract TSelf Instance { get; }
}
