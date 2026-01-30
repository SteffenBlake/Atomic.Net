namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents FlexPositionLeft with nullable components for partial mutations.
/// Matches FlexPositionLeftBehavior structure (Value, Percent).
/// </summary>
public readonly record struct MutFlexPositionLeft(float? Value, bool? Percent);
