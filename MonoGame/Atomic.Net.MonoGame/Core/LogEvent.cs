namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Log event for JSONLogic 'log' operator output.
/// Used to capture logged values during expression evaluation.
/// </summary>
public readonly record struct LogEvent(string Message);
