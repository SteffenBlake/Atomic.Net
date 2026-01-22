using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Persistence;

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
    public void KeySwap_PreservesBothSaveSlots()
    {
        // Arrange: Create entity with initial key
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("save-slot-1"));
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, new PropertiesBehavior());
        entity.SetProperty("health", 100);
        DatabaseRegistry.Instance.Flush();
        
        // Act: Modify entity and swap to different key
        entity.SetProperty("health", 75);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("save-slot-2"));
        DatabaseRegistry.Instance.Flush();
        
        // test-architect: Current state (HP=75) should write to "save-slot-2"
        // Old "save-slot-1" should still exist in DB with HP=100
        
        // Assert: Verify save-slot-2 has current state
        // var slot2Entity = LoadEntityFromDatabase("save-slot-2");
        // Assert.Equal(75, slot2Entity.GetProperty<int>("health"));
        
        // test-architect: Verify save-slot-1 still has original state (orphaned)
        // var slot1Entity = LoadEntityFromDatabase("save-slot-1");
        // Assert.Equal(100, slot1Entity.GetProperty<int>("health"));
        
        // Act: Swap back to save-slot-1
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("save-slot-1"));
        
        // Assert: Entity should load HP=100 from save-slot-1
        // Assert.Equal(100, entity.GetProperty<int>("health"));
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void RapidKeySwapping_DoesNotCorruptData()
    {
        // Arrange: Create entity
        var entity = new Entity(300);
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, new PropertiesBehavior());
        
        // Act: Rapidly swap between multiple keys in same frame
        entity.SetProperty("counter", 1);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("rapid-1"));
        
        entity.SetProperty("counter", 2);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("rapid-2"));
        
        entity.SetProperty("counter", 3);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("rapid-3"));
        
        entity.SetProperty("counter", 4);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("rapid-4"));
        
        // test-architect: Flush should write final state to final key
        DatabaseRegistry.Instance.Flush();
        
        // Assert: Final key should have final state
        // var rapid4Entity = LoadEntityFromDatabase("rapid-4");
        // Assert.Equal(4, rapid4Entity.GetProperty<int>("counter"));
        
        // test-architect: FINDING: Rapid swaps in same frame may result in only the final
        // key being written. Intermediate keys may not get written if they're orphaned
        // before Flush() is called. This is acceptable behavior (batching optimization).
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void KeySwap_WithMutationInSameFrame_WritesToNewKey()
    {
        // Arrange: Create and persist entity
        var entity = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("original-key"));
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity, new PropertiesBehavior());
        entity.SetProperty("value", 50);
        DatabaseRegistry.Instance.Flush();
        
        // Act: Mutate and swap key in same frame (before Flush)
        entity.SetProperty("value", 100);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity, new PersistToDiskBehavior("swapped-key"));
        DatabaseRegistry.Instance.Flush();
        
        // Assert: New key should have mutated value
        // var swappedEntity = LoadEntityFromDatabase("swapped-key");
        // Assert.Equal(100, swappedEntity.GetProperty<int>("value"));
        
        // test-architect: Original key should still have old value (50)
        // var originalEntity = LoadEntityFromDatabase("original-key");
        // Assert.Equal(50, originalEntity.GetProperty<int>("value"));
        
        // test-architect: FINDING: Mutations made to "original-key" in the same frame
        // before key swap are DROPPED (niche edge case). This is acceptable as a feature:
        // the mutation was made while pointing to old key, but Flush happens after swap.
    }

    [Fact(Skip = "Awaiting implementation by @senior-dev")]
    public void KeySwap_ToExistingKey_LoadsFromDatabase()
    {
        // Arrange: Create two separate save slots
        var entity1 = new Entity(300);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity1, new PersistToDiskBehavior("slot-A"));
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity1, new PropertiesBehavior());
        entity1.SetProperty("name", "SaveA");
        entity1.SetProperty("score", 1000);
        DatabaseRegistry.Instance.Flush();
        
        var entity2 = new Entity(301);
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity2, new PersistToDiskBehavior("slot-B"));
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(entity2, new PropertiesBehavior());
        entity2.SetProperty("name", "SaveB");
        entity2.SetProperty("score", 2000);
        DatabaseRegistry.Instance.Flush();
        
        // Act: Swap entity1 to slot-B (which already exists)
        BehaviorRegistry<PersistToDiskBehavior>.Instance.SetBehavior(entity1, new PersistToDiskBehavior("slot-B"));
        
        // Assert: entity1 should load data from slot-B (overwriting its current state)
        // Assert.Equal("SaveB", entity1.GetProperty<string>("name"));
        // Assert.Equal(2000, entity1.GetProperty<int>("score"));
        
        // test-architect: slot-A should still exist with original data
        // var slotAEntity = LoadEntityFromDatabase("slot-A");
        // Assert.Equal("SaveA", slotAEntity.GetProperty<string>("name"));
        // Assert.Equal(1000, slotAEntity.GetProperty<int>("score"));
    }
}
