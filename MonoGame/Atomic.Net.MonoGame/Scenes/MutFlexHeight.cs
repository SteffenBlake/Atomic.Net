namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents FlexHeight with nullable components for partial mutations.
/// Matches FlexHeightBehavior structure (Value, Percent).
/// </summary>
public readonly record struct MutFlexHeight(float? Value, bool? Percent);
