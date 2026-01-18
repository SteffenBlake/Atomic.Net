using Xunit;
using Microsoft.Xna.Framework;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Tests.Transform;

[Collection("NonParallel")]
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
        // Fire reset event to clean up scene entities between tests
        EventBus<ResetEvent>.Push(new());
        _entities.Clear();
    }

    private static void AssertMatricesEqual(Matrix expected, Entity entity)
    {
        var hasTransform = BehaviorRegistry<WorldTransformBehavior>.Instance
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
            actual.M11, actual.M12, actual.M13, actual.M14,
            actual.M21, actual.M22, actual.M23, actual. M24,
            actual.M31, actual. M32, actual.M33, actual.M34,
            actual.M41, actual.M42, actual.M43, actual.M44
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
        // Copilot: INVESTIGATION
        // 1) Fails in group: YES
        // 2) Fails alone: YES
        // 3) Root cause: Anchor point transform not being applied - translation components M41/M42 are (0,0) instead of (5,-5)
        // 4) Fix attempted: None yet - appears to be actual bug in transform calculation, not test pollution
        // 5) @Steffen: The anchor translation is not being applied. Expected: translate(-anchor) * rotate * translate(anchor), but getting rotation-only matrix. Is anchor transform logic implemented?
        //
        // Step 1: Compute expected using XNA
        var expectedMatrix = Matrix.CreateTranslation(10f, 0f, 0f);

        // Step 2: Setup entity and compute using TransformRegistry
        var entity = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = new Vector3(10f, 0f, 0f);
                t.Rotation = Quaternion.Identity;
                t.Scale = Vector3.One;
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
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = Quaternion.Identity;
                t.Scale = new Vector3(2f, 2f, 2f);
                t.Anchor = Vector3.Zero;
            });

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void DirtyParent_UpdatesChildToo()
    {
        // Step 1: Compute expected using XNA
        var expectedMatrix = Matrix.CreateScale(2f, 2f, 2f);

        // Step 2: Setup entity and compute using TransformRegistry
        var parent = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = Quaternion.Identity;
                t.Scale = new Vector3(1f, 1f, 1f);
                t.Anchor = Vector3.Zero;
            });
        var child = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = Quaternion.Identity;
                t.Scale = new Vector3(1f, 1f, 1f);
                t.Anchor = Vector3.Zero;
            })
            .WithParent(parent);

        // Recalc once to make everyone not dirty
        TransformRegistry.Instance.Recalculate();

        // Dirty only the parent
        parent.SetBehavior<TransformBehavior>((ref t) =>
                t.Scale = new Vector3(2f, 2f, 2f)
        );
        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare, child should get updated too
        AssertMatricesEqual(expectedMatrix, child);
    }


    [Fact]
    public void RotationOnly_MatchesXnaRotation()
    {
        // Step 1: Compute expected using XNA
        var rotation = Rotation90DegreesAroundZ();
        var expectedMatrix = Matrix.CreateFromQuaternion(rotation);

        // Step 2: Setup entity and compute using TransformRegistry
        var entity = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = rotation;
                t.Scale = Vector3.One;
                t.Anchor = Vector3.Zero;
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
            .WithTransform((ref t) =>
            {
                t.Position = new Vector3(100f, 0f, 0f);
                t.Rotation = Quaternion.Identity;
                t.Scale = Vector3.One;
                t.Anchor = Vector3.Zero;
            });

        var child = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = new Vector3(10f, 0f, 0f);
                t.Rotation = Quaternion.Identity;
                t.Scale = Vector3.One;
                t.Anchor = Vector3.Zero;
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
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = rotation;
                t.Scale = Vector3.One;
                t.Anchor = Vector3.Zero;
            });

        var child = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = new Vector3(10f, 0f, 0f);
                t.Rotation = Quaternion.Identity;
                t.Scale = Vector3.One;
                t.Anchor = Vector3.Zero;
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
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = rotation;
                t.Scale = Vector3.One;
                t.Anchor = Vector3.Zero;
            });

        var child = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = new Vector3(10f, 0f, 0f);
                t.Rotation = rotation;
                t.Scale = Vector3.One;
                t.Anchor = Vector3.Zero;
            })
            .WithParent(parent);

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        AssertMatricesEqual(expectedChildWorld, child);
    }

    [Fact]
    public void AnchorWithRotation_MatchesXnaTransformOrder()
    {
        // Copilot: INVESTIGATION
        // 1) Fails in group: YES
        // 2) Fails alone: YES
        // 3) Root cause: Anchor point transform not being applied - translation components M41/M42 are (0,0) instead of (5,-5)
        // 4) Fix attempted: None yet - appears to be actual bug in transform calculation, not test pollution
        // 5) @Steffen: The anchor translation is not being applied. Expected: translate(-anchor) * rotate * translate(anchor), but getting rotation-only matrix. Is anchor transform logic implemented?
        
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
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = rotation;
                t.Scale = Vector3.One;
                t.Anchor = new Vector3(5f, 0f, 0f);
            });

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void IdentityTransform_StaysIdentity()
    {
        // Create an entity with default transform (identity)
        var entity = CreateEntity().WithTransform();
        
        TransformRegistry.Instance.Recalculate();
        
        // Should be identity matrix
        var identityMatrix = Matrix.Identity;
        AssertMatricesEqual(identityMatrix, entity);
    }

    [Fact]
    public void ResetPreventsPollution_InGeneral()
    {
        // Step 1: Create entity with rotation (potential polluter)
        var entity1 = CreateEntity();
        entity1.WithTransform((ref t) =>
        {
            var rotation = Rotation90DegreesAroundZ();
            t.Position = Vector3.Zero;
            t.Rotation = rotation;
            t.Scale = Vector3.One;
            t.Anchor = Vector3.Zero;
        });
        TransformRegistry.Instance.Recalculate();
        
        // Verify it has rotation (not identity)
        var hasTransform1 = BehaviorRegistry<WorldTransformBehavior>.Instance
            .TryGetBehavior(entity1, out var wt1);

        Assert.True(hasTransform1);

        var m11_before = wt1!.Value.Value.M11;
        // M11 should NOT be 1.0 (it should be ~0 due to 90 degree rotation)
        Assert.False(
            MathF.Abs(m11_before - 1.0f) < Tolerance, 
            $"M11 should not be identity (1.0), but was {m11_before}"
        );
        
        // Step 2: Reset to clean up
        EventBus<ResetEvent>.Push(new());
        
        // Step 3: Create two new entities with identity transforms
        var entity2 = EntityRegistry.Instance.Activate();
        entity2.WithTransform(); // Default identity transform
        TransformRegistry.Instance.Recalculate();
        
        var entity3 = EntityRegistry.Instance.Activate();
        entity3.WithTransform(); // Default identity transform
        TransformRegistry.Instance.Recalculate();
        
        // Step 4: Verify both are identity (no pollution from entity1's rotation)
        var identityMatrix = Matrix.Identity;
        AssertMatricesEqual(identityMatrix, entity2);
        AssertMatricesEqual(identityMatrix, entity3);
    }

    [Fact]
    public void ResetPreventsPollution_WithExplicitPositionOnly()
    {
        // Step 1: Create entity with complex transform (rotation + position)
        var rotation = Rotation90DegreesAroundZ();
        var entity1 = CreateEntity();
        entity1.WithTransform((ref t) =>
        {
            t.Position = new Vector3(100f, 0f, 0f);
            t.Rotation = rotation;
            t.Scale = Vector3.One;
            t.Anchor = Vector3.Zero;
        });
        TransformRegistry.Instance.Recalculate();
        
        // Step 2: Reset
        EventBus<ResetEvent>.Push(new());
        
        // Step 3: Create new entity with ONLY position (no rotation)
        var entity2 = EntityRegistry.Instance.Activate();
        entity2.WithTransform((ref t) =>
        {
            t.Position = new Vector3(10f, 0f, 0f);
            t.Rotation = Quaternion.Identity;
            t.Scale = Vector3.One;
            t.Anchor = Vector3.Zero;
        });
        TransformRegistry.Instance.Recalculate();
        
        // Step 4: Verify it has translation but NO rotation pollution
        var expectedMatrix = Matrix.CreateTranslation(10f, 0f, 0f);
        AssertMatricesEqual(expectedMatrix, entity2);
    }

    [Fact]
    public void AnchorWithScale_MatchesXnaTransformOrder()
    {
        // Test anchor with scale to ensure anchor offset is scaled correctly
        var anchor = new Vector3(3f, 0f, 0f);
        var scale = new Vector3(2f, 2f, 2f);
        
        // MonoGame calculation: translate to anchor, scale, translate back
        var expectedMatrix = 
            Matrix.CreateTranslation(-anchor) * 
            Matrix.CreateScale(scale) * 
            Matrix.CreateTranslation(anchor);

        var entity = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = Quaternion.Identity;
                t.Scale = new Vector3(2f, 2f, 2f);
                t.Anchor = new Vector3(3f, 0f, 0f);
            });

        TransformRegistry.Instance.Recalculate();

        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void AnchorWithScaleAndRotation_MatchesXnaTransformOrder()
    {
        // Test anchor with both scale and rotation
        var anchor = new Vector3(4f, 2f, 0f);
        var rotation = Rotation90DegreesAroundZ();
        var scale = new Vector3(1.5f, 1.5f, 1f);
        
        // MonoGame calculation
        var expectedMatrix = 
            Matrix.CreateTranslation(-anchor) * 
            Matrix.CreateScale(scale) *
            Matrix.CreateFromQuaternion(rotation) * 
            Matrix.CreateTranslation(anchor);

        var entity = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = rotation;
                t.Scale = new Vector3(1.5f, 1.5f, 1f);
                t.Anchor = new Vector3(4f, 2f, 0f);
            });

        TransformRegistry.Instance.Recalculate();

        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void PositionWithAnchorAndRotation_MatchesXnaTransformOrder()
    {
        // Test full transformation: position + anchor + rotation
        var position = new Vector3(10f, 5f, 0f);
        var anchor = new Vector3(3f, 0f, 0f);
        var rotation = Rotation90DegreesAroundZ();
        
        // MonoGame calculation
        var expectedMatrix = 
            Matrix.CreateTranslation(-anchor) * 
            Matrix.CreateFromQuaternion(rotation) * 
            Matrix.CreateTranslation(anchor) *
            Matrix.CreateTranslation(position);

        var entity = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = new Vector3(10f, 5f, 0f);
                t.Rotation = rotation;
                t.Scale = Vector3.One;
                t.Anchor = new Vector3(3f, 0f, 0f);
            });

        TransformRegistry.Instance.Recalculate();

        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void CompleteTransform_MatchesXnaTransformOrder()
    {
        // Test all components together: position, rotation, scale, anchor
        var position = new Vector3(8f, -3f, 2f);
        var anchor = new Vector3(2f, 1f, 0f);
        var rotation = Rotation90DegreesAroundZ();
        var scale = new Vector3(2f, 3f, 1f);
        
        // MonoGame calculation
        var expectedMatrix = 
            Matrix.CreateTranslation(-anchor) * 
            Matrix.CreateScale(scale) *
            Matrix.CreateFromQuaternion(rotation) * 
            Matrix.CreateTranslation(anchor) *
            Matrix.CreateTranslation(position);

        var entity = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = new Vector3(8f, -3f, 2f);
                t.Rotation = rotation;
                t.Scale = new Vector3(2f, 3f, 1f);
                t.Anchor = new Vector3(2f, 1f, 0f);
            });

        TransformRegistry.Instance.Recalculate();

        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void NonUniformScale_MatchesXnaScale()
    {
        // Test non-uniform scaling without anchor
        var expectedMatrix = Matrix.CreateScale(2f, 3f, 0.5f);

        var entity = CreateEntity()
            .WithTransform((ref t) =>
            {
                t.Position = Vector3.Zero;
                t.Rotation = Quaternion.Identity;
                t.Scale = new Vector3(2f, 3f, 0.5f);
                t.Anchor = Vector3.Zero;
            });

        TransformRegistry.Instance.Recalculate();

        AssertMatricesEqual(expectedMatrix, entity);
    }
}
