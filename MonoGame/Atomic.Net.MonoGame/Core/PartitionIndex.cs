using System.Text.Json.Nodes;
using dotVariant;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Variant type representing partition-aware entity/rule/sequence indices.
/// Global partition uses ushort (0-255), scene partition uses uint (0-based).
/// Provides type safety at compile time to prevent mixing partition types.
/// </summary>
[Variant]
public readonly partial struct PartitionIndex
{
    static partial void VariantOf(ushort global, uint scene);

    /// <summary>
    /// Returns true if this index is in the global partition.
    /// </summary>
    public bool IsGlobal => TryMatch(out ushort _);

    /// <summary>
    /// Converts the partition index to a JsonValue for serialization.
    /// </summary>
    public JsonValue ToJsonValue()
    {
        return Visit(
            static global => JsonValue.Create(global)!,
            static scene => JsonValue.Create(scene)!,
            static () => throw new InvalidOperationException("Invalid PartitionIndex state")
        );
    }
}
