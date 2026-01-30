namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents FlexWidth with nullable components for partial mutations.
/// Matches FlexWidthBehavior structure (Value, Percent).
/// </summary>
public readonly record struct MutFlexWidth(float? Value, bool? Percent);
