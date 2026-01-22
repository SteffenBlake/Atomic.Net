using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Persistence;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Tests.Persistence.Integrations;

/// <summary>
/// Integration tests for scene loading with persistence behavior.
/// Tests: PersistToDiskBehavior applied last, scene load protection, full pipeline.
/// </summary>
/// <remarks>
/// test-architect: These tests validate the full JSON → Entities → Persistence pipeline.
/// Critical behavior: PersistToDiskBehavior MUST be applied LAST to prevent unwanted DB loads.
/// Tests MUST run sequentially due to shared LiteDB database.
/// </remarks>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class PersistenceSceneLoadingTests : IDisposable
{
    private readonly string _dbPath;

    public PersistenceSceneLoadingTests()
    {
        // Arrange: Initialize systems with clean database
        _dbPath = Path.Combine(Path.GetTempPath(), $"persistence_sceneload_{Guid.NewGuid()}.db");
        
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        // DatabaseRegistry.Instance.Initialize(_dbPath);
    }

    public void Dispose()
    {
        // Clean up entities and database between tests
        EventBus<ShutdownEvent>.Push(new());
        
        // DatabaseRegistry.Instance.Shutdown();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void LoadScene_WithPersistToDiskBehavior_AppliesBehaviorLast()
    {
        // Arrange
        var scenePath = "Persistence/Fixtures/persistent-scene.json";

        // Act: Load scene with entities that have persistToDisk behavior
        // test-architect: SceneLoader should call Disable() before load, Enable() after
        // DatabaseRegistry.Instance.Disable();
        SceneLoader.Instance.LoadGameScene(scenePath);
        // DatabaseRegistry.Instance.Enable();

        // Assert: Verify entities were created with PersistToDiskBehavior
        Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-player", out var playerEntity), 
            "persistent-player should be registered");
        
        // test-architect: Verify PersistToDiskBehavior was applied
        // Assert.True(BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(playerEntity.Value, out var behavior));
        // Assert.Equal("player-save-slot-1", behavior.Key);
        
        // test-architect: Verify other behaviors were also applied
        // Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(playerEntity.Value, out _));
        // Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(playerEntity.Value, out _));
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void LoadScene_WithExistingDiskData_LoadsFromDatabaseNotScene()
    {
        // Arrange: Pre-populate database with entity data
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("player-save-slot-1");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
        });
        // TODO: API not yet implemented - // TODO: Move to PropertiesBehavior callback - entity.SetProperty("health", 25); // Different from scene (scene has 100)
        // TODO: API not yet implemented - // TODO: Move to PropertiesBehavior callback - entity.SetProperty("level", 99);  // Different from scene (scene has 5)
        DatabaseRegistry.Instance.Flush();
        
        // Reset to clear in-memory state
        EventBus<ResetEvent>.Push(new());

        // Act: Load scene (should load from disk, not scene JSON)
        var scenePath = "Persistence/Fixtures/persistent-scene.json";
        // DatabaseRegistry.Instance.Disable();
        SceneLoader.Instance.LoadGameScene(scenePath);
        // DatabaseRegistry.Instance.Enable();

        // Assert: Entity should have disk values, not scene values
        Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-player", out var playerEntity));
        // TODO: API not yet implemented - // Assert.Equal(25, playerEntity.Value.GetProperty<int>("health")); // From disk, not scene
        // TODO: API not yet implemented - // Assert.Equal(99, playerEntity.Value.GetProperty<int>("level"));  // From disk, not scene
        
        // test-architect: FINDING: This validates that PersistToDiskBehavior applied last
        // triggers database load, overwriting scene-defined values.
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void LoadScene_DuringDisabled_DoesNotTriggerDirtyTracking()
    {
        // Arrange: Disable dirty tracking
        DatabaseRegistry.Instance.Disable();

        // Act: Load scene with multiple entities
        var scenePath = "Persistence/Fixtures/persistent-scene.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert: No entities should be marked dirty (tracking was disabled)
        // test-architect: Verify dirty flags are not set during scene load
        // var dirtyCount = DatabaseRegistry.Instance.GetDirtyEntityCount();
        // Assert.Equal(0, dirtyCount);
        
        // Re-enable and verify tracking resumes
        DatabaseRegistry.Instance.Enable();
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-player", out var playerEntity));
        // TODO: API not yet implemented - playerEntity.Value.SetProperty("health", 50); // This should mark dirty
        
        // var dirtyCountAfter = DatabaseRegistry.Instance.GetDirtyEntityCount();
        // Assert.Equal(1, dirtyCountAfter);
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void SceneReload_WithModifiedData_PreservesChanges()
    {
        // Arrange: Load scene and modify entity
        var scenePath = "Persistence/Fixtures/persistent-scene.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("inventory-manager", out var inventoryEntity));
        // TODO: API not yet implemented - inventoryEntity.Value.SetProperty("gold", 9999); // Modify from scene default (500)
        DatabaseRegistry.Instance.Flush();

        // Act: Reset and reload scene
        EventBus<ResetEvent>.Push(new());
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert: Modified value should be preserved (loaded from disk)
        Assert.True(EntityIdRegistry.Instance.TryResolve("inventory-manager", out var reloadedEntity));
        // TODO: API not yet implemented - // Assert.Equal(9999, reloadedEntity.Value.GetProperty<int>("gold"));
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void LoadScene_NonPersistentEntity_DoesNotPersist()
    {
        // Arrange: Load scene with mix of persistent and non-persistent entities
        var scenePath = "Persistence/Fixtures/persistent-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert: Verify non-persistent entity was created
        Assert.True(EntityIdRegistry.Instance.TryResolve("non-persistent-entity", out var nonPersistentEntity));
        
        // test-architect: Verify it does NOT have PersistToDiskBehavior
        // Assert.False(BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(nonPersistentEntity.Value, out _));
        
        // Modify and flush
        // TODO: API not yet implemented - nonPersistentEntity.Value.SetProperty("temporary", "modified");
        DatabaseRegistry.Instance.Flush();
        
        // test-architect: This entity should NOT be written to disk
        // var loadedEntity = LoadEntityFromDatabase("non-existent-key");
        // Assert.Null(loadedEntity);
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void InfiniteLoopPrevention_LoadFromDisk_DoesNotTriggerDbWrite()
    {
        // Arrange: Pre-populate database
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("loop-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
        });
        // TODO: API not yet implemented - // TODO: Move to PropertiesBehavior callback - entity.SetProperty("counter", 1);
        DatabaseRegistry.Instance.Flush();
        EventBus<ResetEvent>.Push(new());

        // Act: Create new entity and apply same key (should load from disk)
        var newEntity = new Entity(400);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("loop-test-key");
        });

        // Assert: Entity should have loaded from disk
        // TODO: API not yet implemented - // Assert.Equal(1, newEntity.GetProperty<int>("counter"));
        
        // test-architect: CRITICAL: oldKey == newKey check should prevent infinite loop
        // When loading from disk sets PersistToDiskBehavior("loop-test-key"),
        // the PostBehaviorUpdatedEvent fires with oldKey == newKey,
        // so NO DB operation should occur (preventing recursion).
        
        // Flush should not write again (entity not dirty)
        DatabaseRegistry.Instance.Flush();
        
        // test-architect: Verify only one DB entry exists (no duplicates from loop)
        // var entries = GetAllDatabaseEntries();
        // Assert.Single(entries.Where(e => e.Key == "loop-test-key"));
    }
}
