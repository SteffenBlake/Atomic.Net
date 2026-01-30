namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents a Vector3 with nullable components for partial mutations.
/// Used during entity mutation deserialization to apply only specified fields.
/// </summary>
public readonly record struct MutVector3(float? X, float? Y, float? Z);
