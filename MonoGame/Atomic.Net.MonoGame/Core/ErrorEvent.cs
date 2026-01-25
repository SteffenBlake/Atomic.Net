namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// General-purpose error event for non-critical errors that should be logged.
/// Used for file not found, JSON parse errors, unresolved references, etc.
/// </summary>
public readonly record struct ErrorEvent(string Message);
