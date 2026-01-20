namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Event fired when the entire game is shutting down.
/// Clears both loading and scene entities (complete reset).
/// Used for full cleanup between test runs and game shutdown.
/// </summary>
public readonly record struct ShutdownEvent;
