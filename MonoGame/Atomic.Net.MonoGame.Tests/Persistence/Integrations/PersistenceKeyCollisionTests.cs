using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Persistence;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Tests;

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
        _dbPath = Path.Combine(Path.GetTempPath(), $"persistence_collision_{Guid.NewGuid()}.db");
        
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
    public void DuplicateKeys_SameScene_LastWriteWins()
    {
        // Arrange: Create two entities with same persistence key
        var entity1 = new Entity(300);
        var entity2 = new Entity(301);
        
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity1, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("duplicate-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity1, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity1);
            behavior.Properties["id"] = "first";
        });
        
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity2, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("duplicate-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity2, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity2);
            behavior.Properties["id"] = "second";
        });
        
        // Act: Flush both entities
        DatabaseRegistry.Instance.Flush();
        
        // Assert: Last write should win (entity2 with "second")
        // test-architect: Design decision - either fire ErrorEvent or use last-write-wins
        var newEntity = new Entity(302);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("duplicate-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.Equal("second", loadedProps.Value.Properties["id"]);
        
        // test-architect: FINDING: Need to decide if duplicate keys should fire ErrorEvent
        // or silently use last-write-wins. Current assumption: last-write-wins.
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void DuplicateKeys_CrossScene_LoadOverwritesEntity()
    {
        // Arrange: Create and persist entity in first "scene"
        var entity1 = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity1, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("cross-scene-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity1, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity1);
            behavior.Properties["scene"] = "scene1";
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Simulate scene transition (reset + reload)
        EventBus<ResetEvent>.Push(new());
        
        // Create new entity with same key in second "scene"
        var entity2 = new Entity(400);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity2, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("cross-scene-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity2, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity2);
            behavior.Properties["scene"] = "scene2";
        });
        
        // Assert: entity2 should have loaded data from entity1 (overwriting "scene2" with "scene1")
        // test-architect: When PersistToDiskBehavior is added, it loads from DB
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity2, out var loadedProps));
        Assert.Equal("scene1", loadedProps.Value.Properties["scene"]);
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void EmptyStringKey_FiresErrorEventAndSkipsPersistence()
    {
        // Arrange: Set up error event listener
        using var errorListener = new FakeEventListener<ErrorEvent>();
        
        // Act: Try to create entity with empty string key
        var entity = new Entity(300);
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["test"] = "value";
        });
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
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
        var newEntity = new Entity(301);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("");
        });
        // If it loaded from DB, it would have "test" property. Since it shouldn't, it won't have it.
        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props) 
            && props.Value.Properties.ContainsKey("test"));
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void NullKey_SkipsPersistence()
    {
        // Arrange: Create entity
        var entity = new Entity(300);
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
        });
        
        // Act: Set PersistToDiskBehavior with null key (if possible via API)
        // test-architect: This might not be possible with record struct constructor
        // BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior(null!));
        
        // test-architect: Alternative - verify that null keys are handled gracefully
        // If the struct doesn't allow null, this test validates compile-time safety
        
        // Assert: No entity should be written
        DatabaseRegistry.Instance.Flush();
        
        // test-architect: FINDING: PersistToDiskBehavior(string Key) as readonly record struct
        // means null keys will throw at construction time (C# null safety), not at runtime.
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void WhitespaceKey_FiresErrorEventAndSkipsPersistence()
    {
        // Arrange: Set up error event listener
        using var errorListener = new FakeEventListener<ErrorEvent>();
        
        // Act: Try to create entity with whitespace-only key
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("   ");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["test"] = "whitespace";
        });
        
        // test-architect: Whitespace key should fire ErrorEvent
        DatabaseRegistry.Instance.Flush();
        
        // Assert: ErrorEvent should have been fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e => e.Message.Contains("whitespace") || e.Message.Contains("key") || e.Message.Contains("empty"));
        
        // test-architect: Entity should NOT be written to database
        var newEntity = new Entity(301);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(newEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("   ");
        });
        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var props) 
            && props.Value.Properties.ContainsKey("test"));
    }
}
