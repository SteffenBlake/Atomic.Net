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

public sealed class BehaviorRegistryTests : IDisposable
{
    public BehaviorRegistryTests()
    {
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        EventBus<ResetEvent>.Push(new());
    }

    [Fact]
    public void SetBehavior_StoresBehaviorForEntity()
    {
        var entity = EntityRegistry.Instance.Activate();
        
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity, out var retrieved);
        Assert.True(hasBehavior);
        Assert.Equal(42, retrieved!.Value.Value);
    }

    [Fact]
    public void Remove_RemovesBehaviorFromEntity()
    {
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
        
        BehaviorRegistry<TestBehavior>.Instance.Remove(entity);
        
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
    }

    [Fact]
    public void EntityDeactivation_RemovesBehavior()
    {
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
        
        EntityRegistry.Instance.Deactivate(entity);
        
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
    }

    [Fact]
    public void ResetEvent_DoesNotPolluteBehaviors()
    {
        // Create entity with behavior
        var entity1 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity1, (ref TestBehavior b) => b.Value = 999);
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity1));
        
        // Reset
        EventBus<ResetEvent>.Push(new());
        
        // Behavior should be removed
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity1));
        
        // Create new entity at same index
        var entity2 = EntityRegistry.Instance.Activate();
        Assert.Equal(entity1.Index, entity2.Index);
        
        // Should not have behavior from entity1
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity2));
        
        // Set new behavior
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity2, (ref TestBehavior b) => b.Value = 111);
        
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity2, out var behavior);
        Assert.True(hasBehavior);
        Assert.Equal(111, behavior!.Value.Value);
    }

    [Fact]
    public void HasBehavior_ReturnsTrueWhenPresent()
    {
        var entity = EntityRegistry.Instance.Activate();
        
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
        
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
    }

    [Fact]
    public void TryGetBehavior_ReturnsFalseWhenNotPresent()
    {
        var entity = EntityRegistry.Instance.Activate();
        
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity, out var behavior);
        
        Assert.False(hasBehavior);
        Assert.Null(behavior);
    }

    [Fact]
    public void UpdateBehavior_UpdatesValue()
    {
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 1);
        
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 2);
        
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity, out var behavior);
        Assert.True(hasBehavior);
        Assert.Equal(2, behavior!.Value.Value);
    }

    [Fact]
    public void SetBehavior_FiresBehaviorAddedEvent_OnFirstSet()
    {
        var listener = new EventListener<BehaviorAddedEvent<TestBehavior>>();
        EventBus<BehaviorAddedEvent<TestBehavior>>.Register(listener);
        
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void SetBehavior_FiresPostBehaviorUpdatedEvent_OnSubsequentSet()
    {
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 1);
        
        var listener = new EventListener<PostBehaviorUpdatedEvent<TestBehavior>>();
        EventBus<PostBehaviorUpdatedEvent<TestBehavior>>.Register(listener);
        
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 2);
        
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Remove_FiresBehaviorRemovedEvent()
    {
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
        var listener = new EventListener<BehaviorRemovedEvent<TestBehavior>>();
        EventBus<BehaviorRemovedEvent<TestBehavior>>.Register(listener);
        
        BehaviorRegistry<TestBehavior>.Instance.Remove(entity);
        
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }
}
