namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents a Quaternion with nullable components for partial mutations.
/// Used during entity mutation deserialization to apply only specified fields.
/// </summary>
public readonly record struct MutQuaternion(float? X, float? Y, float? Z, float? W);
