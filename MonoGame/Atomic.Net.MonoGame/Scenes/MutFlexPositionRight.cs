namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents FlexPositionRight with nullable components for partial mutations.
/// Matches FlexPositionRightBehavior structure (Value, Percent).
/// </summary>
public readonly record struct MutFlexPositionRight(float? Value, bool? Percent);
