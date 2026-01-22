using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Persistence;
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
        _dbPath = Path.Combine(Path.GetTempPath(), $"persistence_lifecycle_{Guid.NewGuid()}.db");
        
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        // test-architect: Initialize DatabaseRegistry with test database path
        // DatabaseRegistry.Instance.Initialize(_dbPath); // To be implemented by @senior-dev
    }

    public void Dispose()
    {
        // Clean up ALL entities and shutdown database between tests
        EventBus<ShutdownEvent>.Push(new());
        
        // test-architect: Clean up database file
        // DatabaseRegistry.Instance.Shutdown(); // To be implemented by @senior-dev
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void ResetEvent_WithPersistentEntity_DoesNotDeleteFromDisk()
    {
        // Arrange: Create entity with PersistToDiskBehavior in scene partition
        var entity = new Entity(300); // Scene partition
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("reset-test-key"));
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, new PropertiesBehavior());
        entity.SetProperty("health", 100);
        
        // Act: Flush to write to disk, then fire ResetEvent
        DatabaseRegistry.Instance.Flush();
        EventBus<ResetEvent>.Push(new());
        
        // Assert: Entity should be deactivated in-memory
        Assert.False(entity.IsActive);
        
        // test-architect: Verify database still contains the entity
        // var loadedEntity = LoadEntityFromDatabase("reset-test-key");
        // Assert.NotNull(loadedEntity);
        // Assert.Equal(100, loadedEntity.GetProperty<int>("health"));
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void ShutdownEvent_WithPersistentEntity_DoesNotDeleteFromDisk()
    {
        // Arrange: Create entity with PersistToDiskBehavior
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("shutdown-test-key"));
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, new PropertiesBehavior());
        entity.SetProperty("mana", 50);
        
        // Act: Flush to write to disk, then fire ShutdownEvent
        DatabaseRegistry.Instance.Flush();
        EventBus<ShutdownEvent>.Push(new());
        
        // Assert: All entities should be deactivated
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Empty(activeEntities);
        
        // test-architect: Verify database still contains the entity
        // Restart system and verify data persists
        // AtomicSystem.Initialize();
        // BEDSystem.Initialize();
        // DatabaseRegistry.Instance.Initialize(_dbPath);
        // var loadedEntity = LoadEntityFromDatabase("shutdown-test-key");
        // Assert.NotNull(loadedEntity);
        // Assert.Equal(50, loadedEntity.GetProperty<int>("mana"));
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void EntityDeactivation_WithPersistToDiskBehavior_DoesNotDeleteFromDisk()
    {
        // Arrange: Create and persist entity
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("deactivate-test-key"));
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, new PropertiesBehavior());
        entity.SetProperty("level", 10);
        DatabaseRegistry.Instance.Flush();
        
        // Act: Deactivate entity
        EntityRegistry.Instance.DeactivateEntity(entity);
        
        // Assert: Entity should be inactive
        Assert.False(entity.IsActive);
        
        // test-architect: Verify database still contains the entity
        // var loadedEntity = LoadEntityFromDatabase("deactivate-test-key");
        // Assert.NotNull(loadedEntity);
        // Assert.Equal(10, loadedEntity.GetProperty<int>("level"));
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void BehaviorRemoval_Manual_WritesFinalStateToDisk()
    {
        // Arrange: Create entity with persistence
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("removal-test-key"));
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, new PropertiesBehavior());
        entity.SetProperty("experience", 5000);
        DatabaseRegistry.Instance.Flush();
        
        // Act: Modify entity and manually remove PersistToDiskBehavior
        entity.SetProperty("experience", 10000);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.RemoveBehavior(entity);
        
        // test-architect: Final dirty state should be written on next Flush
        DatabaseRegistry.Instance.Flush();
        
        // Assert: Database should have final state (10000), but entity no longer persists future changes
        // var loadedEntity = LoadEntityFromDatabase("removal-test-key");
        // Assert.Equal(10000, loadedEntity.GetProperty<int>("experience"));
        
        // test-architect: Modify entity again and flush - should NOT update disk
        entity.SetProperty("experience", 20000);
        DatabaseRegistry.Instance.Flush();
        
        // var reloadedEntity = LoadEntityFromDatabase("removal-test-key");
        // Assert.Equal(10000, reloadedEntity.GetProperty<int>("experience")); // Still 10000, not 20000
    }
}
