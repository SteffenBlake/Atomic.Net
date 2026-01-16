using Xunit;
using Microsoft.Xna.Framework;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;

namespace Atomic.Net.MonoGame.Transform.Tests;

public sealed class TransformRegistryTests : IDisposable
{
    private const float Tolerance = 0.0001f;
    private readonly List<Entity> _entities = [];

    public TransformRegistryTests()
    {
        // Trigger initialization to register event handlers
        EventBus<InitializeEvent>.Push(new());
    }

    private Entity CreateEntity()
    {
        var entity = EntityRegistry.Instance.Activate();
        _entities.Add(entity);
        return entity;
    }

    public void Dispose()
    {
        foreach (var entity in _entities)
        {
            EntityRegistry.Instance.Deactivate(entity);
        }
        _entities.Clear();
    }

    private static void AssertMatricesEqual(Matrix expected, Entity entity)
    {
        var hasTransform = RefBehaviorRegistry<WorldTransform>.Instance.TryGetBehavior(entity, out var worldTransform);
        Assert.True(hasTransform, "Entity should have a WorldTransform behavior");
        
        var actual = worldTransform!.Value;
        Assert.Equal(expected.M11, actual.Value.M11.Value, Tolerance);
        Assert.Equal(expected.M12, actual.Value.M12.Value, Tolerance);
        Assert.Equal(expected.M13, actual.Value.M13.Value, Tolerance);
        Assert.Equal(expected.M14, actual.Value.M14.Value, Tolerance);
        Assert.Equal(expected.M21, actual.Value.M21.Value, Tolerance);
        Assert.Equal(expected.M22, actual.Value.M22.Value, Tolerance);
        Assert.Equal(expected.M23, actual.Value.M23.Value, Tolerance);
        Assert.Equal(expected.M24, actual.Value.M24.Value, Tolerance);
        Assert.Equal(expected.M31, actual.Value.M31.Value, Tolerance);
        Assert.Equal(expected.M32, actual.Value.M32.Value, Tolerance);
        Assert.Equal(expected.M33, actual.Value.M33.Value, Tolerance);
        Assert.Equal(expected.M34, actual.Value.M34.Value, Tolerance);
        Assert.Equal(expected.M41, actual.Value.M41.Value, Tolerance);
        Assert.Equal(expected.M42, actual.Value.M42.Value, Tolerance);
        Assert.Equal(expected.M43, actual.Value.M43.Value, Tolerance);
        Assert.Equal(expected.M44, actual.Value.M44.Value, Tolerance);
    }

    private static Quaternion Rotation90DegreesAroundZ() =>
        Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver2);

    [Fact]
    public void PositionOnly_MatchesXnaTranslation()
    {
        // Step 1: Compute expected using XNA
        var expectedMatrix = Matrix.CreateTranslation(10f, 0f, 0f);

        // Step 2: Setup entity and compute using TransformRegistry
        var entity = CreateEntity();
        entity.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Position.X.Value = 10f;
            t.Position.Y.Value = 0f;
            t.Position.Z.Value = 0f;
        });

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void ScaleOnly_MatchesXnaScale()
    {
        // Step 1: Compute expected using XNA
        var expectedMatrix = Matrix.CreateScale(2f, 2f, 2f);

        // Step 2: Setup entity and compute using TransformRegistry
        var entity = CreateEntity();
        entity.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Scale.X.Value = 2f;
            t.Scale.Y.Value = 2f;
            t.Scale.Z.Value = 2f;
        });

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void RotationOnly_MatchesXnaRotation()
    {
        // Step 1: Compute expected using XNA
        var rotation = Rotation90DegreesAroundZ();
        var expectedMatrix = Matrix.CreateFromQuaternion(rotation);

        // Step 2: Setup entity and compute using TransformRegistry
        var entity = CreateEntity();
        entity.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Rotation.X.Value = rotation.X;
            t.Rotation.Y.Value = rotation.Y;
            t.Rotation.Z.Value = rotation.Z;
            t.Rotation.W.Value = rotation.W;
        });

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        // NOTE: If this test fails, it indicates our quaternion-to-matrix conversion
        // uses a different convention than MonoGame. The fix would be to transpose
        // the rotation matrix elements in LocalTransformBlockMapSet.
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void ParentChildPosition_MatchesXnaMultiplication()
    {
        // Step 1: Compute expected using XNA
        var parentMatrix = Matrix.CreateTranslation(100f, 0f, 0f);
        var childLocalMatrix = Matrix.CreateTranslation(10f, 0f, 0f);
        var expectedChildWorld = childLocalMatrix * parentMatrix;

        // Step 2: Setup entities and compute using TransformRegistry
        var parent = CreateEntity();
        parent.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Position.X.Value = 100f;
        });

        var child = CreateEntity();
        child.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Position.X.Value = 10f;
        });
        child.SetParent(parent);

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        AssertMatricesEqual(expectedChildWorld, child);
    }

    [Fact]
    public void ParentRotationAffectsChildPosition_MatchesXnaMultiplication()
    {
        // Step 1: Compute expected using XNA
        var rotation = Rotation90DegreesAroundZ();
        var parentMatrix = Matrix.CreateFromQuaternion(rotation);
        var childLocalMatrix = Matrix.CreateTranslation(10f, 0f, 0f);
        var expectedChildWorld = childLocalMatrix * parentMatrix;

        // Step 2: Setup entities and compute using TransformRegistry
        var parent = CreateEntity();
        parent.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Rotation.X.Value = rotation.X;
            t.Rotation.Y.Value = rotation.Y;
            t.Rotation.Z.Value = rotation.Z;
            t.Rotation.W.Value = rotation.W;
        });

        var child = CreateEntity();
        child.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Position.X.Value = 10f;
        });
        child.SetParent(parent);

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        // NOTE: If child ends up at (0, -10, 0) instead of (0, 10, 0), this confirms
        // the rotation convention issue. The fix is to transpose rotation matrix elements.
        AssertMatricesEqual(expectedChildWorld, child);
    }

    [Fact]
    public void TwoBodyOrbit_MatchesXnaMultiplication()
    {
        // Step 1: Compute expected using XNA
        var rotation = Rotation90DegreesAroundZ();
        var parentMatrix = Matrix.CreateFromQuaternion(rotation);
        var childLocalMatrix = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(10f, 0f, 0f);
        var expectedChildWorld = childLocalMatrix * parentMatrix;

        // Step 2: Setup entities and compute using TransformRegistry
        var parent = CreateEntity();
        parent.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Rotation.X.Value = rotation.X;
            t.Rotation.Y.Value = rotation.Y;
            t.Rotation.Z.Value = rotation.Z;
            t.Rotation.W.Value = rotation.W;
        });

        var child = CreateEntity();
        child.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Position.X.Value = 10f;
            t.Rotation.X.Value = rotation.X;
            t.Rotation.Y.Value = rotation.Y;
            t.Rotation.Z.Value = rotation.Z;
            t.Rotation.W.Value = rotation.W;
        });
        child.SetParent(parent);

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        AssertMatricesEqual(expectedChildWorld, child);
    }

    [Fact]
    public void AnchorWithRotation_MatchesXnaTransformOrder()
    {
        // Step 1: Compute expected using XNA
        // Anchor means: translate to anchor point, rotate, translate back
        var anchor = new Vector3(5f, 0f, 0f);
        var rotation = Rotation90DegreesAroundZ();
        var expectedMatrix = 
            Matrix.CreateTranslation(-anchor) * 
            Matrix.CreateFromQuaternion(rotation) * 
            Matrix.CreateTranslation(anchor);

        // Step 2: Setup entity and compute using TransformRegistry
        var entity = CreateEntity();
        entity.SetRefBehavior((ref readonly TransformBehavior t) =>
        {
            t.Anchor.X.Value = 5f;
            t.Rotation.X.Value = rotation.X;
            t.Rotation.Y.Value = rotation.Y;
            t.Rotation.Z.Value = rotation.Z;
            t.Rotation.W.Value = rotation.W;
        });

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        AssertMatricesEqual(expectedMatrix, entity);
    }
}
