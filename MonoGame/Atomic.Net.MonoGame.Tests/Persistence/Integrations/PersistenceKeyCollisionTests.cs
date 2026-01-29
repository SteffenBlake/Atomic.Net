using System.Collections.Immutable;
using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Persistence;

namespace Atomic.Net.MonoGame.Tests.Persistence.Integrations;

/// <summary>
/// Integration tests for key collision and duplicate key handling.
/// Tests: Duplicate keys in same scene, cross-scene duplicates, empty/null keys.
/// </summary>
/// <remarks>
/// test-architect: These tests validate error handling for invalid persistence keys.
/// Critical behavior: System should fire ErrorEvent for invalid keys, not crash.
/// Tests MUST run sequentially due to shared LiteDB database.
/// </remarks>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class PersistenceKeyCollisionTests : IDisposable
{
    private readonly string _dbPath;

    public PersistenceKeyCollisionTests()
    {
        // Arrange: Initialize systems with clean database
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
    public void DuplicateKeys_SameScene_LastWriteWins()
    {
        // Arrange: Create two entities with same persistence key
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        
        entity1.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("duplicate-key");
        });
        entity1.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("id", "first") };
        });
        
        entity2.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("duplicate-key");
        });
        entity2.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("id", "second") };
        });
        
        // Act: Flush both entities
        DatabaseRegistry.Instance.Flush();
        
        // Assert: Last write should win (entity2 with "second")
        // test-architect: Design decision - either fire ErrorEvent or use last-write-wins
        var newEntity = EntityRegistry.Instance.Activate();
        newEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("duplicate-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.NotNull(loadedProps.Value.Properties);
        Assert.Equal("second", loadedProps.Value.Properties["id"]);
        
        // test-architect: FINDING: Need to decide if duplicate keys should fire ErrorEvent
        // or silently use last-write-wins. Current assumption: last-write-wins.
    }

    [Fact]
    public void DuplicateKeys_CrossScene_LoadOverwritesEntity()
    {
        // Arrange: Create and persist entity in first "scene"
        var entity1 = EntityRegistry.Instance.Activate();
        entity1.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("cross-scene-key");
        });
        entity1.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("scene", "scene1") };
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Simulate scene transition (reset + reload)
        EventBus<ResetEvent>.Push(new());
        
        // Create new entity with same key in second "scene"
        var entity2 = EntityRegistry.Instance.Activate();
        // Add PersistToDiskBehavior LAST to prevent it from loading and then being overwritten
        entity2.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("scene", "scene2") };
        });
        entity2.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("cross-scene-key");
        });
        
        // Assert: entity2 should have loaded data from entity1 (overwriting "scene2" with "scene1")
        // test-architect: When PersistToDiskBehavior is added, it loads from DB
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity2, out var loadedProps));
        Assert.NotNull(loadedProps.Value.Properties);
        Assert.Equal("scene1", loadedProps.Value.Properties["scene"]);
    }

    [Fact]
    public void EmptyStringKey_FiresErrorEventAndSkipsPersistence()
    {
        // Arrange: Set up error event listener
        using var errorListener = new FakeEventListener<ErrorEvent>();
        
        // Act: Try to create entity with empty string key
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("test", "value") };
        });
        entity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("");
        });
        
        // test-architect: Empty key should fire ErrorEvent
        DatabaseRegistry.Instance.Flush();
        
        // Assert: ErrorEvent should have been fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e => e.Message.Contains("empty") || e.Message.Contains("key"));
        
        // test-architect: Entity should NOT be written to database
        // Verify by trying to load with empty key - should not find anything
        var newEntity = EntityRegistry.Instance.Activate();
        newEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("");
        });
        // If it loaded from DB, it would have "test" property. Since it shouldn't, it won't have it.
        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props));
    }

    [Fact]
    public void WhitespaceKey_FiresErrorEventAndSkipsPersistence()
    {
        // Arrange: Set up error event listener
        using var errorListener = new FakeEventListener<ErrorEvent>();
        
        // Act: Try to create entity with whitespace-only key
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("   ");
        });
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("test", "whitespace") };
        });
        
        // test-architect: Whitespace key should fire ErrorEvent
        DatabaseRegistry.Instance.Flush();
        
        // Assert: ErrorEvent should have been fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e => e.Message.Contains("whitespace") || e.Message.Contains("key") || e.Message.Contains("empty"));
        
        // test-architect: Entity should NOT be written to database
        var newEntity = EntityRegistry.Instance.Activate();
        newEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("   ");
        });

        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props));
    }
}
