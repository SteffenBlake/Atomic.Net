using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD computation of WorldTransform = LocalTransform × ParentWorldTransform.
/// </summary>
public sealed class WorldTransformBlockMapSet : ISingleton<WorldTransformBlockMapSet>
{
    public static WorldTransformBlockMapSet Instance { get; } = new();

    // Only 12 computed elements - M14, M24, M34, M44 are constants for affine transforms
    public BlockMapBase M11 { get; }
    public BlockMapBase M12 { get; }
    public BlockMapBase M13 { get; }
    public BlockMapBase M21 { get; }
    public BlockMapBase M22 { get; }
    public BlockMapBase M23 { get; }
    public BlockMapBase M31 { get; }
    public BlockMapBase M32 { get; }
    public BlockMapBase M33 { get; }
    public BlockMapBase M41 { get; }
    public BlockMapBase M42 { get; }
    public BlockMapBase M43 { get; }

    private WorldTransformBlockMapSet()
    {
        var local = LocalTransformBlockMapSet.Instance;
        var parent = ParentWorldTransformBackingStore.Instance;
        
        // Upper-left 3x3: standard matrix multiply
        M11 = BuildDotProduct3(local.M11, local.M12, local.M13, parent.M11, parent.M21, parent.M31);
        M12 = BuildDotProduct3(local.M11, local.M12, local.M13, parent.M12, parent.M22, parent.M32);
        M13 = BuildDotProduct3(local.M11, local.M12, local.M13, parent.M13, parent.M23, parent.M33);

        M21 = BuildDotProduct3(local.M21, local.M22, local.M23, parent.M11, parent.M21, parent.M31);
        M22 = BuildDotProduct3(local.M21, local.M22, local.M23, parent.M12, parent.M22, parent.M32);
        M23 = BuildDotProduct3(local.M21, local.M22, local.M23, parent.M13, parent.M23, parent.M33);

        M31 = BuildDotProduct3(local.M31, local.M32, local.M33, parent.M11, parent.M21, parent.M31);
        M32 = BuildDotProduct3(local.M31, local.M32, local.M33, parent.M12, parent.M22, parent.M32);
        M33 = BuildDotProduct3(local.M31, local.M32, local.M33, parent.M13, parent.M23, parent.M33);

        // Translation row: local.M41*parent.M1x + local.M42*parent.M2x + local.M43*parent.M3x + parent.M4x
        // (since local.M44 = 1, the last term is just parent.M4x)
        M41 = BuildTranslationRow(local.M41, local.M42, local.M43, parent.M11, parent.M21, parent.M31, parent.M41);
        M42 = BuildTranslationRow(local.M41, local.M42, local.M43, parent.M12, parent.M22, parent.M32, parent.M42);
        M43 = BuildTranslationRow(local.M41, local.M42, local.M43, parent.M13, parent.M23, parent.M33, parent.M43);

        // M14, M24, M34, M44 are NOT computed - they are always (0, 0, 0, 1) for affine transforms
    }

    private static BlockMapBase BuildDotProduct3(
        BlockMapBase a1, BlockMapBase a2, BlockMapBase a3,
        BlockMapBase b1, BlockMapBase b2, BlockMapBase b3
    )
    {
        var term1 = new MultiplyBlockMap(a1, b1);
        var term2 = new MultiplyBlockMap(a2, b2);
        var term3 = new MultiplyBlockMap(a3, b3);

        var sum12 = new AddBlockMap(term1, term2);
        return new AddBlockMap(sum12, term3);
    }

    private static BlockMapBase BuildTranslationRow(
        BlockMapBase a1, BlockMapBase a2, BlockMapBase a3,
        BlockMapBase b1, BlockMapBase b2, BlockMapBase b3,
        BlockMapBase add
    )
    {
        // a1*b1 + a2*b2 + a3*b3 + add
        var term1 = new MultiplyBlockMap(a1, b1);
        var term2 = new MultiplyBlockMap(a2, b2);
        var term3 = new MultiplyBlockMap(a3, b3);
        var sum12 = new AddBlockMap(term1, term2);
        var sum123 = new AddBlockMap(sum12, term3);
        return new AddBlockMap(sum123, add);
    }

    public void Recalculate()
    {
        M11.Recalculate();
        M12.Recalculate();
        M13.Recalculate();
        M21.Recalculate();
        M22.Recalculate();
        M23.Recalculate();
        M31.Recalculate();
        M32.Recalculate();
        M33.Recalculate();
        M41.Recalculate();
        M42.Recalculate();
        M43.Recalculate();
    }
}
