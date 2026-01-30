namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents FlexPositionBottom with nullable components for partial mutations.
/// Matches FlexPositionBottomBehavior structure (Value, Percent).
/// </summary>
public readonly record struct MutFlexPositionBottom(float? Value, bool? Percent);
