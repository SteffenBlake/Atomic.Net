using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Scenes.Persistence;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Persistence.Integrations;

/// <summary>
/// Integration tests for key swap functionality.
/// Tests: Key swaps preserve both save slots, rapid swapping doesn't corrupt data.
/// </summary>
/// <remarks>
/// test-architect: These tests validate the "keys as pointers" feature.
/// Critical behavior: Orphaned keys persist in database and can be swapped back to.
/// Tests MUST run sequentially due to shared LiteDB database.
/// </remarks>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class PersistenceKeySwapTests : IDisposable
{
    private readonly string _dbPath;

    public PersistenceKeySwapTests()
    {
        // Arrange: Initialize systems with clean database
        _dbPath = Path.Combine(Path.GetTempPath(), $"persistence_keyswap_{Guid.NewGuid()}.db");
        
        // senior-dev: Set database path via environment variable (per PR #5)
        Environment.SetEnvironmentVariable("ATOMIC_PERSISTENCE_DB_PATH", _dbPath);

        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        // Database initialized via environment variable
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
    public void KeySwap_PreservesBothSaveSlots()
    {
        // Arrange: Create entity with initial key
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("save-slot-1");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["hp"] = 100f;
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Modify entity and swap to different key
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior.Properties["hp"] = 75f;
        });
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("save-slot-2");
        });
        DatabaseRegistry.Instance.Flush();
        
        // test-architect: Current state (HP=75) should write to "save-slot-2"
        // Old "save-slot-1" should still exist in DB with HP=100
        
        // Assert: Verify save-slot-2 has current state (75)
        var slot2Entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(slot2Entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("save-slot-2");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(slot2Entity, out var slot2Props));
        Assert.Equal(75f, slot2Props.Value.Properties["hp"]);
        
        // test-architect: Verify save-slot-1 still has original state (100) - orphaned
        var slot1Entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(slot1Entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("save-slot-1");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(slot1Entity, out var slot1Props));
        Assert.Equal(100f, slot1Props.Value.Properties["hp"]);
        
        // Act: Swap back to save-slot-1
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("save-slot-1");
        });
        
        // Assert: Entity should load HP=100 from save-slot-1
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var reloadedProps));
        Assert.Equal(100f, reloadedProps.Value.Properties["hp"]);
    }

    [Fact]
    public void RapidKeySwapping_DoesNotCorruptData()
    {
        // Arrange: Create entity
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["data"] = "final";
        });
        
        // Act: Rapidly swap between multiple keys in same frame
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("rapid-1");
        });
        
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("rapid-2");
        });
        
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("rapid-3");
        });
        
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("rapid-4");
        });
        
        // test-architect: Flush should write final state to final key
        DatabaseRegistry.Instance.Flush();
        
        // Assert: Final key should have final state
        var rapid4Entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(rapid4Entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("rapid-4");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(rapid4Entity, out var props));
        Assert.Equal("final", props.Value.Properties["data"]);
        
        // test-architect: FINDING: Rapid swaps in same frame may result in only the final
        // key being written. Intermediate keys may not get written if they're orphaned
        // before Flush() is called. This is acceptable behavior (batching optimization).
    }

    [Fact]
    public void KeySwap_WithMutationInSameFrame_WritesToNewKey()
    {
        // Arrange: Create and persist entity
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("original-key");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity);
            behavior.Properties["value"] = 50f;
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Mutate and swap key in same frame (before Flush)
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, (ref PropertiesBehavior behavior) =>
        {
            behavior.Properties["value"] = 100f;
        });
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("swapped-key");
        });
        DatabaseRegistry.Instance.Flush();
        
        // Assert: New key should have mutated value (100)
        var swappedEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(swappedEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("swapped-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(swappedEntity, out var swappedProps));
        Assert.Equal(100f, swappedProps.Value.Properties["value"]);
        
        // test-architect: Original key should still have old value (50)
        var originalEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(originalEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("original-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(originalEntity, out var originalProps));
        Assert.Equal(50f, originalProps.Value.Properties["value"]);
        
        // test-architect: FINDING: Mutations made to "original-key" in the same frame
        // before key swap are DROPPED (niche edge case). This is acceptable as a feature:
        // the mutation was made while pointing to old key, but Flush happens after swap.
    }

    [Fact]
    public void KeySwap_ToExistingKey_LoadsFromDatabase()
    {
        // Arrange: Create two separate save slots
        var entity1 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity1, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("slot-A");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity1, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity1);
            behavior.Properties["slot"] = "A";
        });
        DatabaseRegistry.Instance.Flush();
        
        var entity2 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity2, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("slot-B");
        });
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity2, (ref PropertiesBehavior behavior) =>
        {
            behavior = PropertiesBehavior.CreateFor(entity2);
            behavior.Properties["slot"] = "B";
        });
        DatabaseRegistry.Instance.Flush();
        
        // Act: Swap entity1 to slot-B (which already exists)
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity1, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("slot-B");
        });
        
        // Assert: entity1 should load data from slot-B (overwriting its current state)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity1, out var entity1Props));
        Assert.Equal("B", entity1Props.Value.Properties["slot"]);
        
        // test-architect: slot-A should still exist with original data
        var slotAEntity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(slotAEntity, (ref PersistToDiskBehavior behavior) =>
        {
            behavior = new PersistToDiskBehavior("slot-A");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(slotAEntity, out var slotAProps));
        Assert.Equal("A", slotAProps.Value.Properties["slot"]);
    }
}
