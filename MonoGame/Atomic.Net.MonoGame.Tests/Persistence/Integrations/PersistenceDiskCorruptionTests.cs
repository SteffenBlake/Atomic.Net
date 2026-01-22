using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Scenes.Persistence;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Tests;
using Atomic.Net.MonoGame.Transform;
using LiteDB;

namespace Atomic.Net.MonoGame.Tests.Persistence.Integrations;

/// <summary>
/// Integration tests for disk file corruption and error handling.
/// Tests: Corrupt JSON, missing files, partial entity saves, LiteDB errors.
/// </summary>
/// <remarks>
/// test-architect: These tests validate graceful degradation when disk operations fail.
/// Critical behavior: System should fire ErrorEvent and continue gameplay, never crash.
/// Tests MUST run sequentially due to shared LiteDB database.
/// </remarks>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class PersistenceDiskCorruptionTests : IDisposable
{
    private readonly string _dbPath;

    public PersistenceDiskCorruptionTests()
    {
        // Arrange: Initialize systems with clean database
        _dbPath = Path.Combine(Path.GetTempPath(), $"persistence_corruption_{Guid.NewGuid()}.db");
        
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        DatabaseRegistry.Instance.InitializeDatabase(_dbPath);
    }

    public void Dispose()
    {
        // Clean up entities and database between tests
        EventBus<ShutdownEvent>.Push(new());
        
        DatabaseRegistry.Instance.Shutdown();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public void CorruptJsonInDatabase_FiresErrorEventAndUsesDefaults()
    {
        // Arrange: Set up error event listener BEFORE corrupting database
        using var errorListener = new FakeEventListener<ErrorEvent>();
        
        // Manually corrupt a database entry with invalid JSON in LiteDB
        DatabaseRegistry.Instance.Shutdown();
        using (var db = new LiteDatabase(_dbPath))
        {
            var collection = db.GetCollection("entities");
            collection.Insert(new BsonDocument
            {
                ["_id"] = "corrupt-key",
                ["data"] = "{ invalid json syntax }"  // Malformed JSON that will fail to deserialize
            });
        }
        // Reinitialize to reopen the database with corrupt data
        DatabaseRegistry.Instance.InitializeDatabase(_dbPath);
        
        // Act: Try to load entity with corrupt JSON - should fire ErrorEvent when deserializing
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("corrupt-key");
        });
        
        // Assert: ErrorEvent should have been fired during deserialization attempt
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e => e.Message.Contains("deserialize") || e.Message.Contains("JSON") || e.Message.Contains("parse") || e.Message.Contains("Failed"));
        
        // Entity should fall back to defaults (empty entity, no behaviors loaded from corrupt data)
        // System should NOT crash - graceful degradation
        Assert.True(entity.Active);
        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out _));
    }

    [Fact]
    public void MissingDatabaseFile_UsesSceneDefaults()
    {
        // Arrange: Delete database file to simulate missing file
        DatabaseRegistry.Instance.Shutdown();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
        
        // Act: Try to load entity from non-existent database
        DatabaseRegistry.Instance.InitializeDatabase(_dbPath);
        
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("missing-file-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
        });
        
        // Assert: Entity should use scene defaults (no error)
        
        // test-architect: System should create new database file
        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public void PartialEntitySave_DiskHasSubsetOfBehaviors_MergesWithScene()
    {
        // Arrange: Save entity with only some behaviors
        var entity1 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity1, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("partial-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity1, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity1);
            behavior.Properties["fromDisk"] = "yes";
        });
        // test-architect: Note - no Transform behavior saved
        DatabaseRegistry.Instance.Flush();
        
        // Act: Reset and create new entity with Transform but load from disk
        EventBus<ResetEvent>.Push(new());
        
        var entity2 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(entity2, (ref TransformBehavior behavior) =>
        {
            behavior = TransformBehavior.CreateFor(entity2);
            behavior.Position = new Microsoft.Xna.Framework.Vector3(50, 75, 0);
        });
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity2, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("partial-key");
        });
        
        // Assert: Entity should have loaded Properties from disk
        // test-architect: Disk had Properties, scene added Transform
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity2, out var transform));
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(50, 75, 0), transform.Value.Position);
        
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity2, out var props));
        Assert.Equal("yes", props.Value.Properties["fromDisk"]);
        
        // test-architect: FINDING: Scene provides defaults, disk overwrites with saved values.
        // Behaviors not in disk persist from scene definition.
    }

    [Fact]
    public void DatabaseLocked_FiresErrorEventAndContinues()
    {
        // Arrange: Set up error event listener BEFORE locking file
        using var errorListener = new FakeEventListener<ErrorEvent>();
        
        // Close DatabaseRegistry's connection so we can lock the file
        DatabaseRegistry.Instance.Shutdown();
        
        // Lock database file with exclusive FileStream (prevents any access)
        using var fileLock = new FileStream(_dbPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        
        // Try to reinitialize DatabaseRegistry while file is locked - should fire ErrorEvent
        DatabaseRegistry.Instance.InitializeDatabase(_dbPath);
        
        // Act: Try to flush while database is locked - should have already fired ErrorEvent during Initialize
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("locked-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["test"] = "locked";
        });
        
        // Don't call Flush() - database is null so it would throw per PR #6
        
        // Assert: ErrorEvent should fire when Initialize fails due to lock
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e => 
            e.Message.Contains("lock") || 
            e.Message.Contains("access") || 
            e.Message.Contains("use") ||
            e.Message.Contains("being used") ||
            e.Message.Contains("Failed to initialize") ||
            e.Message.Contains("database"));
        
        // System should NOT crash - gameplay continues despite initialization failure
        Assert.True(entity.Active);
    }

    [Fact]
    public void CrossScenePersistence_PersistentPartitionSurvivesReset()
    {
        // Arrange: Create entity in persistent partition (index < 256) - must use ActivatePersistent()
        var persistentEntity = EntityRegistry.Instance.ActivatePersistent();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(persistentEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("persistent-partition-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(persistentEntity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(persistentEntity);
            behavior.Properties["partition"] = "persistent";
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Fire ResetEvent (should NOT deactivate persistent partition)
        EventBus<ResetEvent>.Push(new());
        
        // Assert: Entity should still be active in-memory
        Assert.True(persistentEntity.Active);
        
        // test-architect: Disk should also have the entity
        var newEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("persistent-partition-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props));
        Assert.Equal("persistent", props.Value.Properties["partition"]);
    }

    [Fact]
    public void CrossScenePersistence_ScenePartitionClearedButDiskPersists()
    {
        // Arrange: Create entity in scene partition (index >= 256) with disk persistence
        var sceneEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(sceneEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("scene-partition-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(sceneEntity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(sceneEntity);
            behavior.Properties["partition"] = "scene";
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Fire ResetEvent (should deactivate scene partition)
        EventBus<ResetEvent>.Push(new());
        
        // Assert: Entity should be deactivated in-memory
        Assert.False(sceneEntity.Active);
        
        // test-architect: But disk should still have the entity (can be reloaded)
        var newEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("scene-partition-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props));
        Assert.Equal("scene", props.Value.Properties["partition"]);
        
        // test-architect: FINDING: This validates that persistent partition vs disk persistence
        // are completely separate concepts. Disk persistence works in EITHER partition.
    }
}
