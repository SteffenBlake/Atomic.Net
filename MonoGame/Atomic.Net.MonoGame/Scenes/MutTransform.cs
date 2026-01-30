namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents transform data with nullable components for partial mutations.
/// Used during entity mutation deserialization to apply only specified transform fields.
/// </summary>
public readonly record struct MutTransform(
    MutVector3? Position,
    MutQuaternion? Rotation,
    MutVector3? Scale,
    MutVector3? Anchor
);
