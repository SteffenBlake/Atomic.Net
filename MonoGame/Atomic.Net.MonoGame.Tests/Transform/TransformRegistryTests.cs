using Xunit;
using Microsoft.Xna.Framework;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Tests.Transform;

public sealed class TransformRegistryTests : IDisposable
{
    private const float Tolerance = 0.0001f;
    private readonly List<Entity> _entities = [];

    public TransformRegistryTests()
    {
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        TransformSystem.Initialize();

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
        var hasTransform = RefBehaviorRegistry<WorldTransformBehavior>.Instance
            .TryGetBehavior(entity, out var worldTransform);

        Assert.True(hasTransform, "Entity should have a WorldTransformBehavior behavior");
        
        var actual = worldTransform!.Value.Value;
        
        // Convert to arrays for easier comparison and clearer error messages
        var expectedArray = new float[]
        {
            expected.M11, expected.M12, expected.M13, expected.M14,
            expected.M21, expected. M22, expected.M23, expected.M24,
            expected.M31, expected.M32, expected.M33, expected. M34,
            expected.M41, expected.M42, expected.M43, expected.M44
        };
        
        var actualArray = new float[]
        {
            actual.M11.Value, actual.M12.Value, actual.M13.Value, actual.M14.Value,
            actual.M21.Value, actual.M22.Value, actual.M23.Value, actual. M24.Value,
            actual.M31.Value, actual. M32.Value, actual.M33.Value, actual.M34.Value,
            actual.M41.Value, actual.M42.Value, actual.M43.Value, actual.M44.Value
        };
        
        var labels = new[]
        {
            "M11", "M12", "M13", "M14",
            "M21", "M22", "M23", "M24",
            "M31", "M32", "M33", "M34",
            "M41", "M42", "M43", "M44"
        };
        
        var mismatches = new List<string>();
        
        for (int i = 0; i < 16; i++)
        {
            if (MathF.Abs(expectedArray[i] - actualArray[i]) > Tolerance)
            {
                mismatches.Add(
                    $"{labels[i]}: expected {expectedArray[i]:F6}, actual {actualArray[i]:F6}"
                );
            }
        }
        
        if (mismatches.Count > 0)
        {
            var message = $@"
                Matrix mismatch:
                Expected: [{string.Join(", ", expectedArray. Select(v => v.ToString("F4")))}]
                Actual:   [{string.Join(", ", actualArray. Select(v => v.ToString("F4")))}]
                Differences:\n  {string.Join("\n  ", mismatches)}";
            Assert.Fail(message);
        }
    }

    private static Quaternion Rotation90DegreesAroundZ() =>
        Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver2);

    [Fact]
    public void PositionOnly_MatchesXnaTranslation()
    {
        // Step 1: Compute expected using XNA
        var expectedMatrix = Matrix.CreateTranslation(10f, 0f, 0f);

        // Step 2: Setup entity and compute using TransformRegistry
        var entity = CreateEntity()
            .WithTransform((ref readonly t) =>
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
        var entity = CreateEntity()
            .WithTransform((ref readonly t) =>
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
        var entity = CreateEntity()
            .WithTransform((ref readonly t) =>
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
        var parent = CreateEntity()
            .WithTransform((ref readonly t) =>
            {
                t.Position.X.Value = 100f;
            });

        var child = CreateEntity()
            .WithTransform((ref readonly t) =>
            {
                t.Position.X.Value = 10f;
            })
            .WithParent(parent);

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
        var parent = CreateEntity()
            .WithTransform((ref readonly t) =>
            {
                t.Rotation.X.Value = rotation.X;
                t.Rotation.Y.Value = rotation.Y;
                t.Rotation.Z.Value = rotation.Z;
                t.Rotation.W.Value = rotation.W;
            });

        var child = CreateEntity()
            .WithTransform((ref readonly t) =>
            {
                t.Position.X.Value = 10f;
            })
            .WithParent(parent);

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
        var parent = CreateEntity()
            .WithTransform((ref readonly t) =>
            {
                t.Rotation.X.Value = rotation.X;
                t.Rotation.Y.Value = rotation.Y;
                t.Rotation.Z.Value = rotation.Z;
                t.Rotation.W.Value = rotation.W;
            });

        var child = CreateEntity()
            .WithTransform((ref readonly t) =>
            {
                t.Position.X.Value = 10f;
                t.Rotation.X.Value = rotation.X;
                t.Rotation.Y.Value = rotation.Y;
                t.Rotation.Z.Value = rotation.Z;
                t.Rotation.W.Value = rotation.W;
            })
            .WithParent(parent);

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
        var entity = CreateEntity()
            .WithTransform((ref readonly t) =>
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
