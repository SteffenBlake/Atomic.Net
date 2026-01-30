namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents FlexPositionTop with nullable components for partial mutations.
/// Matches FlexPositionTopBehavior structure (Value, Percent).
/// </summary>
public readonly record struct MutFlexPositionTop(float? Value, bool? Percent);
