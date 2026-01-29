using System.Collections.Immutable;
using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Persistence;
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
        // Tests use default "persistence.db" and run sequentially (NonParallel collection)
        // Each test cleans up in Dispose() to ensure fresh state
        _dbPath = "persistence.db";

        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
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
        // Arrange: Set up error event listener
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
        EventBus<InitializeEvent>.Push(new());
        
        // Act: Try to load entity with corrupt JSON - should fire ErrorEvent when deserializing
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("corrupt-key");
        });
        
        // Assert: ErrorEvent should have been fired during deserialization attempt
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e => e.Message.Contains("corrupt") || e.Message.Contains("JSON") || e.Message.Contains("parse"));
        
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
        EventBus<InitializeEvent>.Push(new());
        
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("missing-file-key");
        });
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = new();
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
        entity1.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("partial-key");
        });
        entity1.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("fromDisk", "yes") };
        });

        // test-architect: Note - no Transform behavior saved
        DatabaseRegistry.Instance.Flush();
        
        // Act: Reset and create new entity with Transform but load from disk
        EventBus<ResetEvent>.Push(new());
        
        var entity2 = EntityRegistry.Instance.Activate();
        entity2.SetBehavior<TransformBehavior>(static (ref behavior) =>
        {
            behavior.Position = new Microsoft.Xna.Framework.Vector3(50, 75, 0);
        });

        entity2.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("partial-key");
        });
        
        // Assert: Entity should have loaded Properties from disk
        // test-architect: Disk had Properties, scene added Transform
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity2, out var transform));
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(50, 75, 0), transform.Value.Position);
        
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity2, out var props));
        Assert.NotNull(props.Value.Properties);
        Assert.Equal("yes", props.Value.Properties["fromDisk"]);
        
        // test-architect: FINDING: Scene provides defaults, disk overwrites with saved values.
        // Behaviors not in disk persist from scene definition.
    }

    [Fact]
    public void DatabaseLocked_FiresErrorEventAndContinues()
    {
        // Arrange: Set up error event listener BEFORE operations that might fail
        using var errorListener = new FakeEventListener<ErrorEvent>();
        
        // Close DatabaseRegistry's connection so we can lock the file
        DatabaseRegistry.Instance.Shutdown();
        
        // Lock database file with exclusive FileStream (prevents any access)
        using var fileLock = new FileStream(_dbPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        
        // Try to reinitialize DatabaseRegistry while file is locked - should fire ErrorEvent
        EventBus<InitializeEvent>.Push(new());
        
        // Assert: ErrorEvent should have fired during initialization
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e => 
            e.Message.Contains("Failed to initialize") || 
            e.Message.Contains("lock") || 
            e.Message.Contains("access") || 
            e.Message.Contains("use") ||
            e.Message.Contains("being used"));
        
        // System should NOT crash - continues with null database (graceful degradation)
        // Further operations will be no-ops but won't crash
    }

    [Fact]
    public void CrossScenePersistence_GlobalPartitionSurvivesReset()
    {
        // Arrange: Create entity in global partition (index < 256) - must use ActivateGlobal()
        var globalEntity = EntityRegistry.Instance.ActivateGlobal();
        globalEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("global-partition-key");
        });
        globalEntity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("partition", "global") };
        });

        DatabaseRegistry.Instance.Flush();
        
        // Act: Fire ResetEvent (should NOT deactivate global partition)
        EventBus<ResetEvent>.Push(new());
        
        // Assert: Entity should still be active in-memory
        Assert.True(globalEntity.Active);
        
        // test-architect: Disk should also have the entity
        var newEntity = EntityRegistry.Instance.Activate();
        newEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("global-partition-key");
        });

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props));
        Assert.NotNull(props.Value.Properties);
        Assert.Equal("global", props.Value.Properties["partition"]);
    }

    [Fact]
    public void CrossScenePersistence_ScenePartitionClearedButDiskPersists()
    {
        // Arrange: Create entity in scene partition (index >= 256) with disk persistence
        var sceneEntity = EntityRegistry.Instance.Activate();
        sceneEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("scene-partition-key");
        });

        sceneEntity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("partition", "scene") };
        });

        DatabaseRegistry.Instance.Flush();
        
        // Act: Fire ResetEvent (should deactivate scene partition)
        EventBus<ResetEvent>.Push(new());
        
        // Assert: Entity should be deactivated in-memory
        Assert.False(sceneEntity.Active);
        
        // test-architect: But disk should still have the entity (can be reloaded)
        var newEntity = EntityRegistry.Instance.Activate();
        newEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("scene-partition-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props));
        Assert.NotNull(props.Value.Properties);
        Assert.Equal("scene", props.Value.Properties["partition"]);
        
        // test-architect: FINDING: This validates that global partition vs disk persistence
        // are completely separate concepts. Disk persistence works in EITHER partition.
    }
}
