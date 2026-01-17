using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Tests.BED;

// Test behavior for testing - mutable struct
public struct TestBehavior : IBehavior<TestBehavior>
{
    public int Value;
    
    public static TestBehavior CreateFor(Entity entity)
    {
        return new TestBehavior { Value = 0 };
    }
}

[Collection("NonParallel")]
public sealed class BehaviorRegistryTests : IDisposable
{
    public BehaviorRegistryTests()
    {
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        
        // CRITICAL: Must initialize BehaviorRegistry for each behavior type used in tests
        // This registers the BehaviorRegistry to listen for PreEntityDeactivatedEvent
        // Without this, behaviors won't be cleaned up when entities are deactivated!
        BehaviorRegistry<TestBehavior>.Initialize();
        
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        EventBus<ResetEvent>.Push(new());
    }

    [Fact]
    public void SetBehavior_StoresBehaviorForEntity()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 42);
        
        // Assert
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity, out var retrieved);
        Assert.True(hasBehavior);
        Assert.Equal(42, retrieved!.Value.Value);
    }

    [Fact]
    public void Remove_RemovesBehaviorFromEntity()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 42);
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.Remove(entity);
        
        // Assert
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
    }

    [Fact]
    public void EntityDeactivation_RemovesBehavior()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 42);
        
        // Act
        EntityRegistry.Instance.Deactivate(entity);
        
        // Assert - accessing behavior on deactivated entity should throw
        Assert.Throws<InvalidOperationException>(() => 
            BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
    }

    [Fact]
    public void ResetEvent_DoesNotPolluteBehaviors()
    {
        // Arrange
        var entity1 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity1, (ref b) => b.Value = 999);
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity1));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        var entity2 = EntityRegistry.Instance.Activate();
        Assert.Equal(entity1.Index, entity2.Index);
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity2));
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity2, (ref b) => b.Value = 111);
        
        // Assert
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity2, out var behavior);
        Assert.True(hasBehavior);
        Assert.Equal(111, behavior!.Value.Value);
    }

    [Fact]
    public void HasBehavior_ReturnsTrueWhenPresent()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 42);
        
        // Assert
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
    }

    [Fact]
    public void TryGetBehavior_ReturnsFalseWhenNotPresent()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        
        // Act
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity, out var behavior);
        
        // Assert
        Assert.False(hasBehavior);
        Assert.Null(behavior);
    }

    [Fact]
    public void UpdateBehavior_UpdatesValue()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 1);
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 2);
        
        // Assert
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity, out var behavior);
        Assert.True(hasBehavior);
        Assert.Equal(2, behavior!.Value.Value);
    }

    [Fact]
    public void SetBehavior_FiresBehaviorAddedEvent_OnFirstSet()
    {
        // Arrange
        using var listener = new FakeEventListener<BehaviorAddedEvent<TestBehavior>>();
        var entity = EntityRegistry.Instance.Activate();
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 42);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void SetBehavior_FiresPostBehaviorUpdatedEvent_OnSubsequentSet()
    {
        // Arrange
        using var listener = new FakeEventListener<PostBehaviorUpdatedEvent<TestBehavior>>();
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 1);
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 2);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Remove_FiresBehaviorRemovedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<PostBehaviorRemovedEvent<TestBehavior>>();
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref b) => b.Value = 42);
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.Remove(entity);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void StandalonePollutionTest_BehaviorPersistsAcrossReset()
    {
        // Copilot: INVESTIGATION
        // 1) Fails in group: YES
        // 2) Fails alone: NO
        // 3) Root cause: Tries to set behavior on inactive entity (id 35+), suggesting entity pool exhaustion or state corruption
        // 4) Fix attempted: BehaviorRegistry<TestBehavior>.Initialize() already added, but issue persists in group runs
        // 5) @Steffen: This passes alone but fails in group with "inactive entity" error - suggests test execution order creates entity/behavior state mismatch
        
        // DISCOVERED: The pollution was caused by BehaviorRegistry<TestBehavior> not being initialized!
        // Without Initialize(), BehaviorRegistry doesn't register for PreEntityDeactivatedEvent,
        // so it never removes behaviors when entities are deactivated.
        // FIXED: Added BehaviorRegistry<TestBehavior>.Initialize() to test constructor.
        //
        // This test now verifies the cascade works correctly:
        // ResetEvent → EntityRegistry.Deactivate → PreEntityDeactivatedEvent → BehaviorRegistry.Remove
        
        // Set up event listeners to verify cascade
        using var preDeactivatedListener = new FakeEventListener<PreEntityDeactivatedEvent>();
        using var postDeactivatedListener = new FakeEventListener<PostEntityDeactivatedEvent>();
        using var preBehaviorRemovedListener = new FakeEventListener<PreBehaviorRemovedEvent<TestBehavior>>();
        using var postBehaviorRemovedListener = new FakeEventListener<PostBehaviorRemovedEvent<TestBehavior>>();
        
        // Arrange - Create and set a behavior on a scene entity
        var entity1 = EntityRegistry.Instance.Activate();
        var index1 = entity1.Index;
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity1, (ref b) => b.Value = 999);
        
        // Clear any events from setup
        preDeactivatedListener.Clear();
        postDeactivatedListener.Clear();
        preBehaviorRemovedListener.Clear();
        postBehaviorRemovedListener.Clear();
        
        // Act - Fire ResetEvent
        EventBus<ResetEvent>.Push(new());
        
        // Verify cascade happened correctly
        Assert.Single(preDeactivatedListener.ReceivedEvents);
        Assert.Single(postDeactivatedListener.ReceivedEvents);
        Assert.Single(preBehaviorRemovedListener.ReceivedEvents);
        Assert.Single(postBehaviorRemovedListener.ReceivedEvents);
        
        // Create new entity at same index
        var entity2 = EntityRegistry.Instance.Activate();
        Assert.Equal(index1, entity2.Index);
        
        // Assert - The new entity should NOT have the old behavior
        var hasAfterReset = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity2, out var behaviorAfter);
        Assert.False(hasAfterReset, "New entity should not have behavior from previous entity");
    }
}
