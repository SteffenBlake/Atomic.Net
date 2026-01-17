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
        
        // Act
        EntityRegistry.Instance.Deactivate(entity);
        
        // Assert - accessing behavior on deactivated entity should throw
        Assert.Throws<InvalidOperationException>(() => 
            BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity));
    }

    [Fact]
    public void ResetEvent_DoesNotPolluteBehaviors()
    {
        // Copilot: 1) Fails in group: YES - finds behavior when shouldn't 2) Alone: FAILS - same issue 3) Logic alignment: @Steffen - BehaviorRegistry doesn't implement IEventHandler<ResetEvent>, so behaviors persist across resets. Should BehaviorRegistry clear behaviors on ResetEvent?
        
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
        // Copilot: 1) Fails in group: NO - passes 2) Alone: PASSES 3) Logic alignment: YES - HasBehavior should return true after SetBehavior is called
        
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
        // Copilot: 1) Fails in group: YES - returns true when should be false 2) Alone: PASSES 3) Logic alignment: @Steffen - When run in group, TryGetBehavior finds behaviors from previous tests. This suggests BehaviorRegistry state pollution between tests - is there a missing cleanup mechanism?
        
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
        // Copilot: 1) Fails in group: NO - passes 2) Alone: PASSES 3) Logic alignment: YES - test logic matches implementation (SetBehavior should update existing behavior value)
        
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
        // Copilot: 1) Fails in group: YES - no events received 2) Alone: PASSES 3) Logic alignment: @Steffen - Is BehaviorAddedEvent only fired once per BehaviorRegistry instance? The event fires alone but not in group, suggesting event listener pollution or event bus state issues.
        
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
        using var listener = new FakeEventListener<PostBehaviorRemovedEvent<TestBehavior>>();
        var entity = EntityRegistry.Instance.Activate();
        BehaviorRegistry<TestBehavior>.Instance.SetBehavior(entity, (ref TestBehavior b) => b.Value = 42);
        
        // Act
        BehaviorRegistry<TestBehavior>.Instance.Remove(entity);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }
}
