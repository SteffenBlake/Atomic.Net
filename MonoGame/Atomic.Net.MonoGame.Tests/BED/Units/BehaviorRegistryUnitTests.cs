using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Tests.BED.Units;

// Test behavior for testing - mutable struct
public struct TestBehavior
{
    public int Value;

    public static TestBehavior CreateFor(Entity entity)
    {
        return new TestBehavior { Value = 0 };
    }
}

[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class BehaviorRegistryUnitTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    public BehaviorRegistryUnitTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        AtomicSystem.Initialize();

        // CRITICAL: Must initialize BehaviorRegistry for each behavior type used in tests
        // This registers the BehaviorRegistry to listen for PreEntityDeactivatedEvent
        // Without this, behaviors won't be cleaned up when entities are deactivated!
        BehaviorRegistry<TestBehavior>.Initialize();

        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        _errorLogger.Dispose();

        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void SetBehavior_StoresBehaviorForEntity()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();

        // Act
        entity.SetBehavior<TestBehavior>(static (ref b) => b.Value = 42);

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
        entity.SetBehavior<TestBehavior>(static (ref b) => b.Value = 42);
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
        entity.SetBehavior<TestBehavior>(static (ref b) => b.Value = 42);

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
        entity1.SetBehavior<TestBehavior>(static (ref b) => b.Value = 999);
        Assert.True(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity1));

        // Act
        EventBus<ResetEvent>.Push(new());
        var entity2 = EntityRegistry.Instance.Activate();
        Assert.Equal(entity1.Index, entity2.Index);
        Assert.False(BehaviorRegistry<TestBehavior>.Instance.HasBehavior(entity2));
        entity2.SetBehavior<TestBehavior>(static (ref b) => b.Value = 111);

        // Assert
        var hasBehavior = BehaviorRegistry<TestBehavior>.Instance.TryGetBehavior(entity2, out var retrieved);
        Assert.True(hasBehavior);
        Assert.Equal(111, retrieved!.Value.Value);
    }

    [Fact]
    public void SetBehavior_FiresBehaviorAddedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<BehaviorAddedEvent<TestBehavior>>();
        var entity = EntityRegistry.Instance.Activate();

        // Act
        entity.SetBehavior<TestBehavior>(static (ref b) => b.Value = 42);

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
        entity.SetBehavior<TestBehavior>(static (ref b) => b.Value = 42);

        // Act
        BehaviorRegistry<TestBehavior>.Instance.Remove(entity);

        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void UpdateBehavior_FiresBehaviorUpdatedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<PostBehaviorUpdatedEvent<TestBehavior>>();
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TestBehavior>(static (ref b) => b.Value = 42);
        listener.Clear();

        // Act
        entity.SetBehavior<TestBehavior>(static (ref b) => b.Value = 100);

        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }
}
