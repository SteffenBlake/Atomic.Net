namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Command that mutates entity properties via an array of operations.
/// Each operation specifies a target path and a value expression.
/// </summary>
public readonly record struct MutCommand(MutOperation[] Operations);

