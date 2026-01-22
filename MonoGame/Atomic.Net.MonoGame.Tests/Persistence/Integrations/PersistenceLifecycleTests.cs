using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Scenes.Persistence;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Persistence.Integrations;

/// <summary>
/// Integration tests for disk persistence lifecycle edge cases.
/// Tests: ResetEvent, ShutdownEvent, entity deactivation, behavior removal handling.
/// </summary>
/// <remarks>
/// test-architect: These tests validate that disk data persists across entity lifecycle events.
/// Critical behavior: Disk persistence is independent of entity activation state.
/// Tests MUST run sequentially due to shared LiteDB database.
/// </remarks>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class PersistenceLifecycleTests : IDisposable
{
    private readonly string _dbPath;

    public PersistenceLifecycleTests()
    {
        // Arrange: Initialize systems and set up clean database
        _dbPath = "persistence.db";

        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities and shutdown database between tests
        EventBus<ShutdownEvent>.Push(new());
        
        // senior-dev: Clean up database file (Shutdown now implemented)
        DatabaseRegistry.Instance.Shutdown();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public void ResetEvent_WithPersistentEntity_DoesNotDeleteFromDisk()
    {
        // Arrange: Create entity with PersistToDiskBehavior in scene partition
        var entity = EntityRegistry.Instance.Activate(); // senior-dev: Fixed - entities must be activated first
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("reset-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["health"] = 100f;
        });
        
        // Act: Flush to write to disk, then fire ResetEvent
        DatabaseRegistry.Instance.Flush();
        EventBus<ResetEvent>.Push(new());
        
        // Assert: Entity should be deactivated in-memory
        Assert.False(entity.Active);
        
        // test-architect: Verify database still contains the entity by creating a new entity
        // and adding PersistToDiskBehavior with same key - it should load health=100 from disk
        var newEntity = EntityRegistry.Instance.Activate(); // senior-dev: Fixed - entities must be activated first
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("reset-test-key");
        });
        
        // Verify the entity loaded properties from disk
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.Equal(100f, loadedProps.Value.Properties["health"]);
    }

    [Fact]
    public void ShutdownEvent_WithPersistentEntity_DoesNotDeleteFromDisk()
    {
        // Arrange: Create entity with PersistToDiskBehavior
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("shutdown-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["mana"] = 50f;
        });
        
        // Act: Flush to write to disk, then fire ShutdownEvent
        DatabaseRegistry.Instance.Flush();
        EventBus<ShutdownEvent>.Push(new());
        
        // Assert: All entities should be deactivated
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Empty(activeEntities);
        
        // test-architect: Verify database still contains the entity by restarting systems
        // and creating entity with same key - it should load from disk

        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        var newEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("shutdown-test-key");
        });
        
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.Equal(50f, loadedProps.Value.Properties["mana"]);
    }

    [Fact]
    public void EntityDeactivation_WithPersistToDiskBehavior_DoesNotDeleteFromDisk()
    {
        // Arrange: Create and persist entity
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("deactivate-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["level"] = 10f;
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Deactivate entity
        entity.Deactivate();
        
        // Assert: Entity should be inactive
        Assert.False(entity.Active);
        
        // test-architect: Verify database still contains the entity
        // Create new entity with same key and verify it loads from disk
        var newEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("deactivate-test-key");
        });
        
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.Equal(10f, loadedProps.Value.Properties["level"]);
    }

    [Fact]
    public void BehaviorRemoval_Manual_WritesFinalStateToDisk()
    {
        // Arrange: Create entity with persistence
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("removal-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["experience"] = 5000f;
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Modify entity, remove PersistToDiskBehavior, then flush
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior.Properties["experience"] = 10000f;
        });
        entity.RemoveBehavior<PersistToDiskBehavior>();
        
        // test-architect: Final dirty state (10000) should be written on next Flush
        DatabaseRegistry.Instance.Flush();
        
        // Assert: Database should have final state (10000)
        var newEntity1 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity1, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("removal-test-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity1, out var props1));
        Assert.Equal(10000f, props1.Value.Properties["experience"]);
        
        // test-architect: Modify entity again and flush - should NOT update disk (no PersistToDiskBehavior)
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior.Properties["experience"] = 20000f;
        });
        DatabaseRegistry.Instance.Flush();
        
        // Verify disk still has 10000 (not 20000)
        var newEntity2 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity2, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("removal-test-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity2, out var props2));
        Assert.Equal(10000f, props2.Value.Properties["experience"]);
    }
}
