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
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert: Verify entities were created with PersistToDiskBehavior
        Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-player", out var playerEntity), 
            "persistent-player should be registered");
        
        // test-architect: Verify PersistToDiskBehavior was applied
        Assert.True(BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(playerEntity.Value, out var persistBehavior));
        Assert.Equal("player-save-slot-1", persistBehavior.Value.Key);
        
        // test-architect: Verify other behaviors were also applied (PersistToDiskBehavior should be LAST)
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(playerEntity.Value, out _));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(playerEntity.Value, out _));
        
        // test-architect: CRITICAL - The test verifies behavior exists, but actual ORDER verification
        // requires checking SceneLoader code has comment "PersistToDiskBehavior MUST be applied last"
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void LoadScene_WithExistingDiskData_LoadsFromDatabaseNotScene()
    {
        // Arrange: Pre-populate database with DIFFERENT data than scene
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("player-save-slot-1");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["fromDisk"] = "yes";  // This value is from disk
            behavior.Properties["hp"] = 999f;         // Different from scene
        });
        DatabaseRegistry.Instance.Flush();
        
        // Reset to clear in-memory state
        EventBus<ResetEvent>.Push(new());

        // Act: Load scene (scene would have different values, but disk should override)
        var scenePath = "Persistence/Fixtures/persistent-scene.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert: Entity should have DISK values, NOT scene values
        Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-player", out var playerEntity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(playerEntity.Value, out var props));
        Assert.Equal("yes", props.Value.Properties["fromDisk"]);  // From disk, not scene
        Assert.Equal(999f, props.Value.Properties["hp"]);        // From disk (999), not scene default
        
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
        // test-architect: We can't directly check dirty count without exposing internal state
        // Instead, verify that mutations during disabled don't persist
        Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-player", out var playerEntity));
        
        // Mutate entity while still disabled
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(playerEntity.Value, (ref PropertiesBehavior behavior) =>
        {
            behavior.Properties["duringDisabled"] = "should-not-persist";
        });
        DatabaseRegistry.Instance.Flush(); // Should be no-op (disabled)
        
        // Re-enable and verify tracking resumes
        DatabaseRegistry.Instance.Enable();
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(playerEntity.Value, (ref PropertiesBehavior behavior) =>
        {
            behavior.Properties["afterEnabled"] = "should-persist";
        });
        DatabaseRegistry.Instance.Flush(); // Should write now
        
        // Verify: Load entity from disk and check what persisted
        var newEntity = new Entity(301);
        Assert.True(BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(playerEntity.Value, out var persistKey));
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior(persistKey.Value.Key);
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props));
        Assert.False(props.Value.Properties.ContainsKey("duringDisabled")); // Not persisted
        Assert.Equal("should-persist", props.Value.Properties["afterEnabled"]); // Persisted
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void SceneReload_WithModifiedData_PreservesChanges()
    {
        // Arrange: Load scene and modify entity
        var scenePath = "Persistence/Fixtures/persistent-scene.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("inventory-manager", out var inventoryEntity));
        
        // Modify entity data
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(inventoryEntity.Value, (ref PropertiesBehavior behavior) =>
        {
            behavior.Properties["modified"] = "yes";
            behavior.Properties["value"] = 777f;
        });
        DatabaseRegistry.Instance.Flush();

        // Act: Reset and reload scene
        EventBus<ResetEvent>.Push(new());
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert: Modified value should be preserved (loaded from disk, NOT scene defaults)
        Assert.True(EntityIdRegistry.Instance.TryResolve("inventory-manager", out var reloadedEntity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(reloadedEntity.Value, out var props));
        Assert.Equal("yes", props.Value.Properties["modified"]);  // From disk
        Assert.Equal(777f, props.Value.Properties["value"]);      // From disk, not scene
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
        Assert.False(BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(nonPersistentEntity.Value, out _));
        
        // Modify entity and flush
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(nonPersistentEntity.Value, (ref PropertiesBehavior behavior) =>
        {
            behavior.Properties["test"] = "should-not-save";
        });
        DatabaseRegistry.Instance.Flush();
        
        // Reset and try to load - should NOT find anything (no PersistToDiskBehavior)
        EventBus<ResetEvent>.Push(new());
        
        // Create new entity and try to add PersistToDiskBehavior with non-existent key
        var testEntity = new Entity(301);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(testEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("non-persistent-entity-key");
        });
        // Should load empty/default since non-persistent entity was never saved
        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(testEntity, out var props) 
            && props.Value.Properties.ContainsKey("test"));
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void InfiniteLoopPrevention_LoadFromDisk_DoesNotTriggerDbWrite()
    {
        // Arrange: Pre-populate database with unique marker
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("loop-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["originalData"] = "first-write";
            behavior.Properties["counter"] = 1f;
        });
        DatabaseRegistry.Instance.Flush();
        EventBus<ResetEvent>.Push(new());

        // Act: Create new entity with DIFFERENT data, then apply same key
        // Key application should LOAD from disk (overwriting the different data)
        var newEntity = new Entity(400);
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(newEntity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(newEntity);
            behavior.Properties["originalData"] = "second-write"; // Different from disk
            behavior.Properties["counter"] = 2f; // Different from disk
        });
        
        // Now apply PersistToDiskBehavior - should load from disk, triggering PostBehaviorUpdatedEvent
        // with oldKey == newKey, preventing infinite loop
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("loop-test-key");
        });

        // Assert: Entity should have loaded data from disk (NOT the "second-write")
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props));
        Assert.Equal("first-write", props.Value.Properties["originalData"]); // From disk
        Assert.Equal(1f, props.Value.Properties["counter"]); // From disk
        
        // test-architect: CRITICAL: oldKey == newKey check should prevent infinite loop
        // When loading from disk sets PersistToDiskBehavior("loop-test-key"),
        // the PostBehaviorUpdatedEvent fires with oldKey == newKey,
        // so NO DB operation should occur (preventing recursion).
        
        // Flush should not write again (entity not dirty)
        DatabaseRegistry.Instance.Flush();
        
        // Verify data is still correct (no corruption from loop)
        var verifyEntity = new Entity(401);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(verifyEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("loop-test-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(verifyEntity, out var verifyProps));
        Assert.Equal("first-write", verifyProps.Value.Properties["originalData"]);
    }
}
