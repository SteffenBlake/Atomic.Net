using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Scenes;

/// <summary>
/// Integration tests for JSON scene loading system.
/// Tests the full pipeline: JSON → Entities → Behaviors → Systems
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class SceneLoaderIntegrationTests : IDisposable
{
    public SceneLoaderIntegrationTests()
    {
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both loading and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void LoadGameScene_WithBasicEntities_SpawnsEntitiesInScenePartition()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        
        // test-architect: Should spawn 2 entities (root-container, menu-button)
        Assert.True(activeEntities.Count >= 2, $"Expected at least 2 entities, got {activeEntities.Count}");
        
        // test-architect: All entities should be in scene partition (index >= 32)
        Assert.All(activeEntities, entity => 
            Assert.True(entity.Index >= Constants.MaxLoadingEntities, 
                $"Entity {entity.Index} should be in scene partition (>= {Constants.MaxLoadingEntities})"));
    }

    [Fact]
    public void LoadLoadingScene_WithBasicEntities_SpawnsEntitiesInLoadingPartition()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadLoadingScene(scenePath);

        // Assert
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        
        // test-architect: Should spawn 2 entities (root-container, menu-button)
        Assert.True(activeEntities.Count >= 2, $"Expected at least 2 entities, got {activeEntities.Count}");
        
        // test-architect: All entities should be in loading partition (index < 32)
        Assert.All(activeEntities, entity => 
            Assert.True(entity.Index < Constants.MaxLoadingEntities, 
                $"Entity {entity.Index} should be in loading partition (< {Constants.MaxLoadingEntities})"));
    }

    [Fact]
    public void LoadGameScene_WithEntityIds_RegistersEntitiesById()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify "root-container" can be resolved
        Assert.True(EntityIdRegistry.Instance.TryResolve("root-container", out var rootEntity), "root-container should be registered");
        Assert.True(rootEntity.Value.Index >= Constants.MaxLoadingEntities);

        // test-architect: Verify "menu-button" can be resolved
        Assert.True(EntityIdRegistry.Instance.TryResolve("menu-button", out var buttonEntity), "menu-button should be registered");
        Assert.True(buttonEntity.Value.Index >= Constants.MaxLoadingEntities);
    }

    [Fact]
    public void LoadGameScene_WithTransformBehavior_AppliesTransformToEntities()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("root-container", out var rootEntity));

        // test-architect: Verify root-container has transform with full data
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(rootEntity.Value, out var rootTransform), "root-container should have TransformBehavior");
        Assert.NotNull(rootTransform);
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(0, 0, 0), rootTransform.Value.Position);
        Assert.Equal(Microsoft.Xna.Framework.Quaternion.Identity, rootTransform.Value.Rotation);
        Assert.Equal(Microsoft.Xna.Framework.Vector3.One, rootTransform.Value.Scale);

        // test-architect: Verify menu-button has transform with position only (rest are defaults)
        Assert.True(EntityIdRegistry.Instance.TryResolve("menu-button", out var buttonEntity));
        
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(buttonEntity.Value, out var buttonTransform), "menu-button should have TransformBehavior");
        Assert.NotNull(buttonTransform);
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(100, 50, 0), buttonTransform.Value.Position);
        Assert.Equal(Microsoft.Xna.Framework.Quaternion.Identity, buttonTransform.Value.Rotation);
        Assert.Equal(Microsoft.Xna.Framework.Vector3.One, buttonTransform.Value.Scale);
    }

    [Fact]
    public void LoadGameScene_WithParentReferences_EstablishesParentChildRelationships()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("root-container", out var rootEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("menu-button", out var buttonEntity));

        // test-architect: Verify menu-button has root-container as parent
        Assert.True(buttonEntity.Value.TryGetParent(out var parent), "menu-button should have parent");
        Assert.NotNull(parent);
        Assert.Equal(rootEntity.Value.Index, parent.Value.Index);

        // test-architect: Verify root-container has menu-button as child
        var rootChildren = rootEntity.Value.GetChildren().ToList();
        Assert.Single(rootChildren);
        Assert.Contains(buttonEntity.Value, rootChildren);
    }

    [Fact]
    public void LoadGameScene_WithDuplicateIds_FirstWriteWins()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/duplicate-ids.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("duplicate", out var entity), "duplicate ID should be registered");

        // test-architect: Verify first entity won (position [0, 0, 0], not [100, 100, 0])
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.NotNull(transform);
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(0, 0, 0), transform.Value.Position);
    }

    [Fact]
    public void LoadGameScene_WithUnresolvedParent_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "Scenes/Fixtures/unresolved-parent.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should fire error event for unresolved parent
        Assert.NotEmpty(errorListener.ReceivedEvents);
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("missing-parent") || e.Message.Contains("Unresolved"));
        Assert.NotEqual(default, errorEvent);
        
        // test-architect: Entity should still be spawned, just without parent
        Assert.True(EntityIdRegistry.Instance.TryResolve("orphan", out var orphanEntity), "orphan entity should still be spawned");
        Assert.False(orphanEntity.Value.TryGetParent(out _), "orphan should not have parent since reference failed");
    }

    [Fact]
    public void LoadGameScene_WithMissingFile_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "Scenes/Fixtures/does-not-exist.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should fire error event for missing file
        Assert.NotEmpty(errorListener.ReceivedEvents);
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("not found") || e.Message.Contains("file"));
        Assert.NotEqual(default, errorEvent);
        
        // test-architect: No entities should be spawned
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Empty(activeEntities);
    }

    [Fact]
    public void LoadGameScene_WithInvalidJson_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "Scenes/Fixtures/invalid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should fire error event for invalid JSON
        Assert.NotEmpty(errorListener.ReceivedEvents);
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("JSON") || e.Message.Contains("parse"));
        Assert.NotEqual(default, errorEvent);
        
        // test-architect: No entities should be spawned
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Empty(activeEntities);
    }

    [Fact]
    public void ResetEvent_ClearsEntityIdRegistry()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("root-container", out _), "ID should be registered before reset");

        // Act
        EventBus<ResetEvent>.Push(new());

        // Assert
        // test-architect: IDs should be cleared after reset
        Assert.False(EntityIdRegistry.Instance.TryResolve("root-container", out _), "ID should be cleared after reset");
    }

    [Fact]
    public void ResetEvent_DoesNotPollute_SceneLoading()
    {
        // Arrange: Load scene first time
        var scenePath = "Scenes/Fixtures/basic-scene.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("root-container", out var entity1), "root-container should resolve on first load");
        var firstIndex = entity1.Value.Index;
        
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity1.Value, out var transform1), "root-container should have transform on first load");

        // Act: Reset and reload
        EventBus<ResetEvent>.Push(new());
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert: Second load should work identically
        Assert.True(EntityIdRegistry.Instance.TryResolve("root-container", out var entity2), "root-container should resolve on second load");
        Assert.Equal(firstIndex, entity2.Value.Index);
        
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity2.Value, out var transform2), "root-container should have transform on second load");
        Assert.NotNull(transform2);
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(0, 0, 0), transform2.Value.Position);
        Assert.Equal(Microsoft.Xna.Framework.Quaternion.Identity, transform2.Value.Rotation);
        Assert.Equal(Microsoft.Xna.Framework.Vector3.One, transform2.Value.Scale);
    }

    [Fact]
    public void EntityIdRegistry_WhenIdBehaviorChanges_UpdatesRegistryCorrectly()
    {
        // Arrange: Create entity with initial ID
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<IdBehavior>.Instance.SetBehavior(entity, (ref IdBehavior behavior) => 
            behavior = new IdBehavior("initial-id"));
        
        // Assert: Initial ID resolves
        Assert.True(EntityIdRegistry.Instance.TryResolve("initial-id", out var resolvedEntity1), "initial-id should be registered");
        Assert.Equal(entity.Index, resolvedEntity1.Value.Index);

        // Act: Change the ID by removing and re-adding the behavior
        BehaviorRegistry<IdBehavior>.Instance.Remove(entity);
        BehaviorRegistry<IdBehavior>.Instance.SetBehavior(entity, (ref IdBehavior behavior) => 
            behavior = new IdBehavior("new-id"));
        
        // Assert: Old ID no longer resolves
        Assert.False(EntityIdRegistry.Instance.TryResolve("initial-id", out _), "initial-id should be removed from registry");
        
        // Assert: New ID resolves to same entity
        Assert.True(EntityIdRegistry.Instance.TryResolve("new-id", out var resolvedEntity2), "new-id should be registered");
        Assert.Equal(entity.Index, resolvedEntity2.Value.Index);
    }
}
