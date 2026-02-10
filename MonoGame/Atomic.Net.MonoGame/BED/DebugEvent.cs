namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Event for debugging purposes. MUST be removed before committing code.
/// Used to trace execution flow and state during test debugging.
/// </summary>
public readonly record struct DebugEvent(string Message);
