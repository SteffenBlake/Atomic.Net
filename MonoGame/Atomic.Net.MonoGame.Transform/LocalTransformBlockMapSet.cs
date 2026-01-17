using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD computation of LocalTransform from Position, Rotation, Scale, Anchor.
/// </summary>
public sealed class LocalTransformBlockMapSet : ISingleton<LocalTransformBlockMapSet>
{
    internal static void Initialize()
    {
        Instance = new();
    }

    public static LocalTransformBlockMapSet Instance { get; private set; } = null!;

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
        var rotX = TransformBackingStore.Instance.RotationX;
        var rotY = TransformBackingStore.Instance.RotationY;
        var rotZ = TransformBackingStore.Instance.RotationZ;
        var rotW = TransformBackingStore.Instance.RotationW;

        var scaleX = TransformBackingStore.Instance.ScaleX;
        var scaleY = TransformBackingStore.Instance.ScaleY;
        var scaleZ = TransformBackingStore.Instance.ScaleZ;

        var posX = TransformBackingStore.Instance.PositionX;
        var posY = TransformBackingStore.Instance.PositionY;
        var posZ = TransformBackingStore.Instance.PositionZ;

        var anchorX = TransformBackingStore.Instance.AnchorX;
        var anchorY = TransformBackingStore.Instance.AnchorY;
        var anchorZ = TransformBackingStore.Instance.AnchorZ;

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
        M12 = new MultiplyBlockMap(scaleX, r21);
        M13 = new MultiplyBlockMap(scaleX, r31);

        M21 = new MultiplyBlockMap(scaleY, r12);
        M22 = new MultiplyBlockMap(scaleY, r22);
        M23 = new MultiplyBlockMap(scaleY, r32);

        M31 = new MultiplyBlockMap(scaleZ, r13);
        M32 = new MultiplyBlockMap(scaleZ, r23);
        M33 = new MultiplyBlockMap(scaleZ, r33);

        // Translation row with anchor transformation
        // Formula: position + anchor - (rotationScale * anchor)
        // The rotation-scale matrix is stored in transpose form, so to multiply RS * anchor,
        // we access matrix columns: M11,M21,M31 is column 1 (X), M12,M22,M32 is column 2 (Y), etc.
        // M41 = posX + anchorX - (M11*anchorX + M21*anchorY + M31*anchorZ)
        var transformedAnchorX = new AddBlockMap(
            new AddBlockMap(
                new MultiplyBlockMap(M11, anchorX),
                new MultiplyBlockMap(M21, anchorY)
            ),
            new MultiplyBlockMap(M31, anchorZ)
        );
        var translationX = new SubtractBlockMap(
            new AddBlockMap(posX, anchorX),
            transformedAnchorX
        );

        // M42 = posY + anchorY - (M12*anchorX + M22*anchorY + M32*anchorZ)
        var transformedAnchorY = new AddBlockMap(
            new AddBlockMap(
                new MultiplyBlockMap(M12, anchorX),
                new MultiplyBlockMap(M22, anchorY)
            ),
            new MultiplyBlockMap(M32, anchorZ)
        );
        var translationY = new SubtractBlockMap(
            new AddBlockMap(posY, anchorY),
            transformedAnchorY
        );

        // M43 = posZ + anchorZ - (M13*anchorX + M23*anchorY + M33*anchorZ)
        var transformedAnchorZ = new AddBlockMap(
            new AddBlockMap(
                new MultiplyBlockMap(M13, anchorX),
                new MultiplyBlockMap(M23, anchorY)
            ),
            new MultiplyBlockMap(M33, anchorZ)
        );
        var translationZ = new SubtractBlockMap(
            new AddBlockMap(posZ, anchorZ),
            transformedAnchorZ
        );

        M41 = translationX;
        M42 = translationY;
        M43 = translationZ;

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
