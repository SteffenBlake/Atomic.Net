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
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
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
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
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
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
        
        // Act
        EntityRegistry.Instance.Deactivate(entity);
        
        // Assert
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
    }

    [Fact]
    public void ResetEvent_DoesNotPolluteBehaviors()
    {
        // Arrange
        var entity1 = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity1, (ref TestBehavior b) => b.Value = 999);
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity1));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        var entity2 = EntityRegistry.Instance.Activate();
        Assert.Equal(entity1.Index, entity2.Index);
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity2));
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity2, (ref TestBehavior b) => b.Value = 111);
        
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
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
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
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 1);
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 2);
        
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
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
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
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 1);
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 2);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Remove_FiresBehaviorRemovedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<BehaviorRemovedEvent<TestBehavior>>();
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.Remove(entity);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }
}
