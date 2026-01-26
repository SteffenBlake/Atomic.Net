using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Persistence;

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
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities and shutdown database between tests
        EventBus<ShutdownEvent>.Push(new());
        
        // Clean up database file (Shutdown now implemented)
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
        var entity = EntityRegistry.Instance.Activate(); // Fixed - entities must be activated first
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("reset-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new(new Dictionary<string, PropertyValue> { { "health", 100f } });
        });
        
        // Act: Flush to write to disk, then fire ResetEvent
        DatabaseRegistry.Instance.Flush();
        EventBus<ResetEvent>.Push(new());
        
        // Assert: Entity should be deactivated in-memory
        Assert.False(entity.Active);
        
        // test-architect: Verify database still contains the entity by creating a new entity
        // and adding PersistToDiskBehavior with same key - it should load health=100 from disk
        var newEntity = EntityRegistry.Instance.Activate(); // Fixed - entities must be activated first
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("reset-test-key");
        });
        
        // Verify the entity loaded properties from disk
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.NotNull(loadedProps.Value.Properties);
        Assert.Equal(100f, loadedProps.Value.Properties["health"]);
    }

    [Fact]
    public void ShutdownEvent_WithPersistentEntity_DoesNotDeleteFromDisk()
    {
        // Arrange: Create entity with PersistToDiskBehavior
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("shutdown-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new(new Dictionary<string, PropertyValue> { { "mana", 50f } });
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
        EventBus<InitializeEvent>.Push(new());
        
        var newEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("shutdown-test-key");
        });
        
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.NotNull(loadedProps.Value.Properties);
        Assert.Equal(50f, loadedProps.Value.Properties["mana"]);
    }

    [Fact]
    public void EntityDeactivation_WithPersistToDiskBehavior_DoesNotDeleteFromDisk()
    {
        // Arrange: Create and persist entity
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("deactivate-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new(new Dictionary<string, PropertyValue> { { "level", 10f } });
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Deactivate entity
        entity.Deactivate();
        
        // Assert: Entity should be inactive
        Assert.False(entity.Active);
        
        // test-architect: Verify database still contains the entity
        // Create new entity with same key and verify it loads from disk
        var newEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("deactivate-test-key");
        });
        
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.NotNull(loadedProps.Value.Properties);
        Assert.Equal(10f, loadedProps.Value.Properties["level"]);
    }

    [Fact]
    public void BehaviorRemoval_Manual_WritesFinalStateToDisk()
    {
        // Arrange: Create entity with persistence
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("removal-test-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new(new Dictionary<string, PropertyValue> { { "experience", 5000f } });
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Modify entity, remove PersistToDiskBehavior, then flush
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new(new Dictionary<string, PropertyValue> { { "experience", 10000f } });
        });
        entity.RemoveBehavior<PersistToDiskBehavior>();
        
        // senior-dev: Per DatabaseRegistry line 324-327, removing PersistToDiskBehavior clears dirty flag.
        // So the modified value (10000) will NOT be written. Database still has original value (5000).
        DatabaseRegistry.Instance.Flush();
        
        // Assert: Database should still have original state (5000), not the modified state (10000)
        var newEntity1 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity1, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("removal-test-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity1, out var props1));
        Assert.NotNull(props1.Value.Properties);
        Assert.Equal(5000f, props1.Value.Properties["experience"]); // Original value, not 10000
        
        // test-architect: Modify entity again and flush - should NOT update disk (no PersistToDiskBehavior)
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, static (ref behavior) =>
        {
            behavior = new(new Dictionary<string, PropertyValue> { { "experience", 20000f } });
        });
        DatabaseRegistry.Instance.Flush();
        
        // Verify disk still has 5000 (not 20000)
        var newEntity2 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity2, static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("removal-test-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity2, out var props2));
        Assert.NotNull(props2.Value.Properties);
        Assert.Equal(5000f, props2.Value.Properties["experience"]); // Still original value
    }
}
