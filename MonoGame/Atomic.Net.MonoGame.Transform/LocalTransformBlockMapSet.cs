using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD computation of LocalTransform from Position, Rotation, Scale, Anchor.
/// </summary>
public sealed class LocalTransformBlockMapSet : ISingleton<LocalTransformBlockMapSet>
{
    public static LocalTransformBlockMapSet Instance { get; } = new();

    // Only 12 computed elements - M14, M24, M34, M44 are constants (0, 0, 0, 1) for affine transforms
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

    private LocalTransformBlockMapSet()
    {
        var store = TransformBackingStore.Instance;
        
        var rotX = store.RotationX;
        var rotY = store.RotationY;
        var rotZ = store.RotationZ;
        var rotW = store.RotationW;

        var scaleX = store.ScaleX;
        var scaleY = store.ScaleY;
        var scaleZ = store.ScaleZ;

        var posX = store.PositionX;
        var posY = store.PositionY;
        var posZ = store.PositionZ;

        // Constants using InputFloatMap (single float, not dense array)
        var one = new InputFloatMap(1f);
        var two = new InputFloatMap(2f);

        // Precompute quaternion products
        var xx = new MultiplyBlockMap(rotX, rotX);
        var yy = new MultiplyBlockMap(rotY, rotY);
        var zz = new MultiplyBlockMap(rotZ, rotZ);
        var xy = new MultiplyBlockMap(rotX, rotY);
        var xz = new MultiplyBlockMap(rotX, rotZ);
        var yz = new MultiplyBlockMap(rotY, rotZ);
        var wx = new MultiplyBlockMap(rotW, rotX);
        var wy = new MultiplyBlockMap(rotW, rotY);
        var wz = new MultiplyBlockMap(rotW, rotZ);

        // 2 * products
        var twoXX = new MultiplyScalarBlockMap(xx, two);
        var twoYY = new MultiplyScalarBlockMap(yy, two);
        var twoZZ = new MultiplyScalarBlockMap(zz, two);
        var twoXY = new MultiplyScalarBlockMap(xy, two);
        var twoXZ = new MultiplyScalarBlockMap(xz, two);
        var twoYZ = new MultiplyScalarBlockMap(yz, two);
        var twoWX = new MultiplyScalarBlockMap(wx, two);
        var twoWY = new MultiplyScalarBlockMap(wy, two);
        var twoWZ = new MultiplyScalarBlockMap(wz, two);

        // Rotation matrix elements (unscaled)
        // R11 = 1 - 2(y² + z²)
        var yyPlusZZ = new AddBlockMap(twoYY, twoZZ);
        var r11 = new ScalarSubtractBlockMap(one, yyPlusZZ);

        // R12 = 2(xy - wz)
        var r12 = new SubtractBlockMap(twoXY, twoWZ);

        // R13 = 2(xz + wy)
        var r13 = new AddBlockMap(twoXZ, twoWY);

        // R21 = 2(xy + wz)
        var r21 = new AddBlockMap(twoXY, twoWZ);

        // R22 = 1 - 2(x² + z²)
        var xxPlusZZ = new AddBlockMap(twoXX, twoZZ);
        var r22 = new ScalarSubtractBlockMap(one, xxPlusZZ);

        // R23 = 2(yz - wx)
        var r23 = new SubtractBlockMap(twoYZ, twoWX);

        // R31 = 2(xz - wy)
        var r31 = new SubtractBlockMap(twoXZ, twoWY);

        // R32 = 2(yz + wx)
        var r32 = new AddBlockMap(twoYZ, twoWX);

        // R33 = 1 - 2(x² + y²)
        var xxPlusYY = new AddBlockMap(twoXX, twoYY);
        var r33 = new ScalarSubtractBlockMap(one, xxPlusYY);

        // Apply scale to rotation matrix
        M11 = new MultiplyBlockMap(scaleX, r11);
        M12 = new MultiplyBlockMap(scaleX, r12);
        M13 = new MultiplyBlockMap(scaleX, r13);

        M21 = new MultiplyBlockMap(scaleY, r21);
        M22 = new MultiplyBlockMap(scaleY, r22);
        M23 = new MultiplyBlockMap(scaleY, r23);

        M31 = new MultiplyBlockMap(scaleZ, r31);
        M32 = new MultiplyBlockMap(scaleZ, r32);
        M33 = new MultiplyBlockMap(scaleZ, r33);

        // Translation row - just position directly
        M41 = posX;
        M42 = posY;
        M43 = posZ;

        // M14, M24, M34, M44 are NOT computed - they are constants (0, 0, 0, 1) for affine transforms
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
