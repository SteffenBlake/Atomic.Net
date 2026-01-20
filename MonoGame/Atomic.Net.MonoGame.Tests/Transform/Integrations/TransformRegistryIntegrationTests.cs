using Xunit;
using Microsoft.Xna.Framework;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Transform;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class TransformRegistryIntegrationTests : IDisposable
{
    private const float Tolerance = 0.0001f;

    public TransformRegistryIntegrationTests()
    {
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        TransformSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both loading and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
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
            expected.M21, expected.M22, expected.M23, expected.M24,
            expected.M31, expected.M32, expected.M33, expected.M34,
            expected.M41, expected.M42, expected.M43, expected.M44
        };
        
        var actualArray = new float[]
        {
            actual.M11, actual.M12, actual.M13, actual.M14,
            actual.M21, actual.M22, actual.M23, actual.M24,
            actual.M31, actual.M32, actual.M33, actual.M34,
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
                Expected: [{string.Join(", ", expectedArray.Select(v => v.ToString("F4")))}]
                Actual:   [{string.Join(", ", actualArray.Select(v => v.ToString("F4")))}]
                Differences:\n  {string.Join("\n  ", mismatches)}";
            Assert.Fail(message);
        }
    }

    private static Quaternion Rotation90DegreesAroundZ() =>
        Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver2);

    [Fact]
    public void PositionOnly_MatchesXnaTranslation()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/position-only.json";
        var expectedMatrix = Matrix.CreateTranslation(10f, 20f, 30f);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void ScaleOnly_MatchesXnaScale()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/scale-only.json";
        var expectedMatrix = Matrix.CreateScale(2f, 3f, 4f);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void DirtyParent_UpdatesChildToo()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/dirty-parent-child.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var parentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent);
        var childResolved = EntityIdRegistry.Instance.TryResolve("child", out var child);
        Assert.True(parentResolved && childResolved);
        
        TransformRegistry.Instance.Recalculate();
        
        // Modify parent transform to make it dirty
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(parent, (ref t) =>
        {
            t.Position = new Vector3(200f, 0f, 0f);
        });
        
        TransformRegistry.Instance.Recalculate();

        // Assert - child should have updated world transform
        var expectedChild = Matrix.CreateTranslation(250f, 0f, 0f);
        AssertMatricesEqual(expectedChild, child);
    }

    [Fact]
    public void RotationOnly_MatchesXnaRotation()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/rotation-only.json";
        var expectedMatrix = Matrix.CreateFromQuaternion(Rotation90DegreesAroundZ());

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void ParentChildPosition_MatchesXnaMultiplication()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/parent-child-position.json";
        var expectedChild = Matrix.CreateTranslation(15f, 0f, 0f);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var parentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent);
        var childResolved = EntityIdRegistry.Instance.TryResolve("child", out var child);
        Assert.True(parentResolved && childResolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expectedChild, child);
    }

    [Fact]
    public void ParentRotationAffectsChildPosition_MatchesXnaMultiplication()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/parent-rotation-affects-child.json";
        
        var parentTransform = Matrix.CreateTranslation(100f, 0f, 0f) * 
                             Matrix.CreateFromQuaternion(Rotation90DegreesAroundZ());
        var localChild = Matrix.CreateTranslation(50f, 0f, 0f);
        var expectedChild = localChild * parentTransform;

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var parentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent);
        var childResolved = EntityIdRegistry.Instance.TryResolve("child", out var child);
        Assert.True(parentResolved && childResolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expectedChild, child);
    }

    [Fact]
    public void TwoBodyOrbit_MatchesXnaMultiplication()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/two-body-orbit.json";
        
        var sunTransform = Matrix.Identity;
        var earthLocal = Matrix.CreateTranslation(100f, 0f, 0f) * 
                        Matrix.CreateFromQuaternion(Rotation90DegreesAroundZ());
        var earthWorld = earthLocal * sunTransform;
        var moonLocal = Matrix.CreateTranslation(20f, 0f, 0f);
        var expectedMoon = moonLocal * earthWorld;

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var sunResolved = EntityIdRegistry.Instance.TryResolve("sun", out var sun);
        var earthResolved = EntityIdRegistry.Instance.TryResolve("earth", out var earth);
        var moonResolved = EntityIdRegistry.Instance.TryResolve("moon", out var moon);
        Assert.True(sunResolved && earthResolved && moonResolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expectedMoon, moon);
    }

    [Fact]
    public void AnchorWithRotation_MatchesXnaTransformOrder()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/anchor-with-rotation.json";
        
        var anchor = new Vector3(5f, -5f, 0f);
        var rotation = Rotation90DegreesAroundZ();
        var expected = Matrix.CreateTranslation(-anchor) *
                      Matrix.CreateFromQuaternion(rotation) *
                      Matrix.CreateTranslation(anchor);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expected, entity);
    }

    [Fact]
    public void IdentityTransform_StaysIdentity()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/identity-transform.json";
        var expectedMatrix = Matrix.Identity;

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void ResetPreventsPollution_InGeneral()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/position-only.json";

        // Act - Load scene, modify, reset, load again
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity1Resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity1);
        Assert.True(entity1Resolved);
        
        TransformRegistry.Instance.Recalculate();
        
        EventBus<ResetEvent>.Push(new());
        
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity2Resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity2);
        Assert.True(entity2Resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert - entity should have clean transform
        var expectedMatrix = Matrix.CreateTranslation(10f, 20f, 30f);
        AssertMatricesEqual(expectedMatrix, entity2);
    }

    [Fact]
    public void ResetPreventsPollution_WithExplicitPositionOnly()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/position-only.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity1Resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity1);
        Assert.True(entity1Resolved);
        
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(entity1, (ref t) =>
        {
            t.Position = new Vector3(999f, 999f, 999f);
        });
        
        TransformRegistry.Instance.Recalculate();
        
        EventBus<ResetEvent>.Push(new());
        
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity2Resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity2);
        Assert.True(entity2Resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        var expectedMatrix = Matrix.CreateTranslation(10f, 20f, 30f);
        AssertMatricesEqual(expectedMatrix, entity2);
    }

    [Fact]
    public void AnchorWithScale_MatchesXnaTransformOrder()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/anchor-with-scale.json";
        
        var anchor = new Vector3(10f, 20f, 30f);
        var scale = new Vector3(2f, 3f, 4f);
        var expected = Matrix.CreateTranslation(-anchor) *
                      Matrix.CreateScale(scale) *
                      Matrix.CreateTranslation(anchor);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expected, entity);
    }

    [Fact]
    public void AnchorWithScaleAndRotation_MatchesXnaTransformOrder()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/anchor-scale-rotation.json";
        
        var anchor = new Vector3(5f, -5f, 0f);
        var scale = new Vector3(2f, 2f, 2f);
        var rotation = Rotation90DegreesAroundZ();
        var expected = Matrix.CreateTranslation(-anchor) *
                      Matrix.CreateScale(scale) *
                      Matrix.CreateFromQuaternion(rotation) *
                      Matrix.CreateTranslation(anchor);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expected, entity);
    }

    [Fact]
    public void PositionWithAnchorAndRotation_MatchesXnaTransformOrder()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/position-anchor-rotation.json";
        
        var position = new Vector3(100f, 200f, 300f);
        var anchor = new Vector3(5f, -5f, 0f);
        var rotation = Rotation90DegreesAroundZ();
        var expected = Matrix.CreateTranslation(-anchor) *
                      Matrix.CreateFromQuaternion(rotation) *
                      Matrix.CreateTranslation(anchor) *
                      Matrix.CreateTranslation(position);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expected, entity);
    }

    [Fact]
    public void CompleteTransform_MatchesXnaTransformOrder()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/complete-transform.json";
        
        var position = new Vector3(100f, 200f, 300f);
        var rotation = Quaternion.Normalize(new Quaternion(0.1f, 0.2f, 0.3f, 0.9273619f));
        var scale = new Vector3(2f, 3f, 4f);
        var anchor = new Vector3(10f, 20f, 30f);
        
        var expected = Matrix.CreateTranslation(-anchor) *
                      Matrix.CreateScale(scale) *
                      Matrix.CreateFromQuaternion(rotation) *
                      Matrix.CreateTranslation(anchor) *
                      Matrix.CreateTranslation(position);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expected, entity);
    }

    [Fact]
    public void NonUniformScale_MatchesXnaScale()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/non-uniform-scale.json";
        var expectedMatrix = Matrix.CreateScale(1f, 2f, 3f);

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var resolved = EntityIdRegistry.Instance.TryResolve("entity", out var entity);
        Assert.True(resolved);
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        AssertMatricesEqual(expectedMatrix, entity);
    }

    [Fact]
    public void PartialDirtyTree_OnlyDirtyGrandchild_UpdatesCorrectly()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/partial-dirty-three-levels.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root", out var root);
        var middleResolved = EntityIdRegistry.Instance.TryResolve("middle", out var middle);
        var leafResolved = EntityIdRegistry.Instance.TryResolve("leaf", out var leaf);
        Assert.True(rootResolved && middleResolved && leafResolved);
        
        TransformRegistry.Instance.Recalculate();
        
        // Modify only leaf
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(leaf, (ref t) =>
        {
            t.Position = new Vector3(40f, 0f, 0f);
        });
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        var expectedLeaf = Matrix.CreateTranslation(70f, 0f, 0f);
        AssertMatricesEqual(expectedLeaf, leaf);
    }

    [Fact]
    public void PartialDirtyTree_OnlyDirtyMiddleNode_UpdatesChildrenCorrectly()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/partial-dirty-three-levels.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root", out var root);
        var middleResolved = EntityIdRegistry.Instance.TryResolve("middle", out var middle);
        var leafResolved = EntityIdRegistry.Instance.TryResolve("leaf", out var leaf);
        Assert.True(rootResolved && middleResolved && leafResolved);
        
        TransformRegistry.Instance.Recalculate();
        
        // Modify middle node
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(middle, (ref t) =>
        {
            t.Position = new Vector3(25f, 0f, 0f);
        });
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        var expectedLeaf = Matrix.CreateTranslation(65f, 0f, 0f);
        AssertMatricesEqual(expectedLeaf, leaf);
    }

    [Fact]
    public void PartialDirtyTree_DirtyRootWithMultipleLevels_UpdatesAllDescendants()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/partial-dirty-three-levels.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root", out var root);
        var middleResolved = EntityIdRegistry.Instance.TryResolve("middle", out var middle);
        var leafResolved = EntityIdRegistry.Instance.TryResolve("leaf", out var leaf);
        Assert.True(rootResolved && middleResolved && leafResolved);
        
        TransformRegistry.Instance.Recalculate();
        
        // Modify root
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(root, (ref t) =>
        {
            t.Position = new Vector3(100f, 0f, 0f);
        });
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        var expectedLeaf = Matrix.CreateTranslation(150f, 0f, 0f);
        AssertMatricesEqual(expectedLeaf, leaf);
    }

    [Fact]
    public void PartialDirtyTree_MultipleRecalculations_MaintainsCorrectState()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/partial-dirty-three-levels.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root", out var root);
        var middleResolved = EntityIdRegistry.Instance.TryResolve("middle", out var middle);
        var leafResolved = EntityIdRegistry.Instance.TryResolve("leaf", out var leaf);
        Assert.True(rootResolved && middleResolved && leafResolved);
        
        TransformRegistry.Instance.Recalculate();
        TransformRegistry.Instance.Recalculate();
        TransformRegistry.Instance.Recalculate();

        // Assert - should still be correct after multiple recalculations
        var expectedLeaf = Matrix.CreateTranslation(60f, 0f, 0f);
        AssertMatricesEqual(expectedLeaf, leaf);
    }

    [Fact]
    public void PartialDirtyTree_DeepHierarchyWithDirtyMiddleNodes_UpdatesCorrectly()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/deep-hierarchy.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root", out var root);
        var level2Resolved = EntityIdRegistry.Instance.TryResolve("level2", out var level2);
        var level4Resolved = EntityIdRegistry.Instance.TryResolve("level4", out var level4);
        Assert.True(rootResolved && level2Resolved && level4Resolved);
        
        TransformRegistry.Instance.Recalculate();
        
        // Modify level2
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(level2, (ref t) =>
        {
            t.Position = new Vector3(20f, 0f, 0f);
        });
        
        TransformRegistry.Instance.Recalculate();

        // Assert
        var expectedLevel4 = Matrix.CreateTranslation(60f, 0f, 0f);
        AssertMatricesEqual(expectedLevel4, level4);
    }

    [Fact]
    public void PartialDirtyTree_SiblingsDontAffectEachOther()
    {
        // Arrange
        var scenePath = "Transform/Fixtures/siblings.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root", out var root);
        var leftResolved = EntityIdRegistry.Instance.TryResolve("left-child", out var leftChild);
        var rightResolved = EntityIdRegistry.Instance.TryResolve("right-child", out var rightChild);
        Assert.True(rootResolved && leftResolved && rightResolved);
        
        TransformRegistry.Instance.Recalculate();
        
        // Modify left child
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(leftChild, (ref t) =>
        {
            t.Position = new Vector3(100f, 10f, 0f);
        });
        
        TransformRegistry.Instance.Recalculate();

        // Assert - right child should be unchanged
        var expectedRight = Matrix.CreateTranslation(30f, -10f, 0f);
        AssertMatricesEqual(expectedRight, rightChild);
    }
}
