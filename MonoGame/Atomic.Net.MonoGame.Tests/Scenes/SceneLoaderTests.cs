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
public sealed class SceneLoaderTests : IDisposable
{
    public SceneLoaderTests()
    {
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up scene entities between tests
        EventBus<ResetEvent>.Push(new());
    }

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader.LoadGameScene")]
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

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader.LoadLoadingScene")]
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

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader and EntityIdRegistry")]
    public void LoadGameScene_WithEntityIds_RegistersEntitiesById()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify "root-container" can be resolved
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root-container", out var rootEntity);
        Assert.True(rootResolved, "root-container should be registered");
        Assert.True(rootEntity.Index >= Constants.MaxLoadingEntities);

        // test-architect: Verify "menu-button" can be resolved
        var buttonResolved = EntityIdRegistry.Instance.TryResolve("menu-button", out var buttonEntity);
        Assert.True(buttonResolved, "menu-button should be registered");
        Assert.True(buttonEntity.Index >= Constants.MaxLoadingEntities);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader")]
    public void LoadGameScene_WithTransformBehavior_AppliesTransformToEntities()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root-container", out var rootEntity);
        Assert.True(rootResolved);

        // test-architect: Verify root-container has transform with full data
        var rootHasTransform = BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(rootEntity, out var rootTransform);
        Assert.True(rootHasTransform, "root-container should have TransformBehavior");
        Assert.NotNull(rootTransform);
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(0, 0, 0), rootTransform.Value.Position);
        Assert.Equal(Microsoft.Xna.Framework.Quaternion.Identity, rootTransform.Value.Rotation);
        Assert.Equal(Microsoft.Xna.Framework.Vector3.One, rootTransform.Value.Scale);

        // test-architect: Verify menu-button has transform with position only (rest are defaults)
        var buttonResolved = EntityIdRegistry.Instance.TryResolve("menu-button", out var buttonEntity);
        Assert.True(buttonResolved);
        
        var buttonHasTransform = BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(buttonEntity, out var buttonTransform);
        Assert.True(buttonHasTransform, "menu-button should have TransformBehavior");
        Assert.NotNull(buttonTransform);
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(100, 50, 0), buttonTransform.Value.Position);
        Assert.Equal(Microsoft.Xna.Framework.Quaternion.Identity, buttonTransform.Value.Rotation);
        Assert.Equal(Microsoft.Xna.Framework.Vector3.One, buttonTransform.Value.Scale);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader")]
    public void LoadGameScene_WithParentReferences_EstablishesParentChildRelationships()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        var rootResolved = EntityIdRegistry.Instance.TryResolve("root-container", out var rootEntity);
        var buttonResolved = EntityIdRegistry.Instance.TryResolve("menu-button", out var buttonEntity);
        Assert.True(rootResolved && buttonResolved);

        // test-architect: Verify menu-button has root-container as parent
        var buttonHasParent = buttonEntity.TryGetParent(out var parent);
        Assert.True(buttonHasParent, "menu-button should have parent");
        Assert.NotNull(parent);
        Assert.Equal(rootEntity.Index, parent.Value.Index);

        // test-architect: Verify root-container has menu-button as child
        var rootChildren = rootEntity.GetChildren().ToList();
        Assert.Single(rootChildren);
        Assert.Contains(buttonEntity, rootChildren);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader")]
    public void LoadGameScene_WithDuplicateIds_FirstWriteWins()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/duplicate-ids.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        var resolved = EntityIdRegistry.Instance.TryResolve("duplicate", out var entity);
        Assert.True(resolved, "duplicate ID should be registered");

        // test-architect: Verify first entity won (position [0, 0, 0], not [100, 100, 0])
        var hasTransform = BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transform);
        Assert.True(hasTransform);
        Assert.NotNull(transform);
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(0, 0, 0), transform.Value.Position);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader")]
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
        var resolved = EntityIdRegistry.Instance.TryResolve("orphan", out var orphanEntity);
        Assert.True(resolved, "orphan entity should still be spawned");
        var hasParent = orphanEntity.TryGetParent(out _);
        Assert.False(hasParent, "orphan should not have parent since reference failed");
    }

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader")]
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

    [Fact(Skip = "Waiting for @senior-dev to implement SceneLoader")]
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

    [Fact(Skip = "Waiting for @senior-dev to implement EntityIdRegistry cleanup")]
    public void ResetEvent_ClearsEntityIdRegistry()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/basic-scene.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var beforeReset = EntityIdRegistry.Instance.TryResolve("root-container", out _);
        Assert.True(beforeReset, "ID should be registered before reset");

        // Act
        EventBus<ResetEvent>.Push(new());

        // Assert
        // test-architect: IDs should be cleared after reset
        var afterReset = EntityIdRegistry.Instance.TryResolve("root-container", out _);
        Assert.False(afterReset, "ID should be cleared after reset");
    }
}
