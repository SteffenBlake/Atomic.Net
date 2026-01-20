using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;

namespace Atomic.Net.MonoGame.Tests.BED;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class HierarchyRegistryTests : IDisposable
{
    public HierarchyRegistryTests()
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
    public void WithParent_SetsParentChildRelationship()
    {
        // Arrange
        var parent = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate();
        
        // Act
        child.WithParent(parent);
        
        // Assert
        Assert.True(child.TryGetParent(out var retrievedParent));
        Assert.Equal(parent.Index, retrievedParent.Value.Index);
    }

    [Fact]
    public void GetChildren_ReturnsChildEntities()
    {
        // Arrange
        var parent = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate().WithParent(parent);
        var child2 = EntityRegistry.Instance.Activate().WithParent(parent);
        var child3 = EntityRegistry.Instance.Activate().WithParent(parent);
        
        // Act
        var children = parent.GetChildren().ToList();
        
        // Assert
        Assert.Equal(3, children.Count);
        Assert.Contains(child1, children);
        Assert.Contains(child2, children);
        Assert.Contains(child3, children);
    }

    [Fact]
    public void RemoveParent_ClearsParentRelationship()
    {
        // Arrange
        var parent = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate().WithParent(parent);
        Assert.True(child.TryGetParent(out _));
        
        // Act
        BehaviorRegistry<Parent>.Instance.Remove(child);
        
        // Assert
        Assert.False(child.TryGetParent(out _));
    }

    [Fact]
    public void ParentDeactivation_OrphansChildren()
    {
        // Arrange
        var parent = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate().WithParent(parent);
        var child2 = EntityRegistry.Instance.Activate().WithParent(parent);
        Assert.True(child1.TryGetParent(out _));
        Assert.True(child2.TryGetParent(out _));
        
        // Act
        EntityRegistry.Instance.Deactivate(parent);
        
        // Assert
        Assert.False(child1.TryGetParent(out _));
        Assert.False(child2.TryGetParent(out _));
    }

    [Fact]
    public void ChildDeactivation_RemovesFromParentChildren()
    {
        // Arrange
        var parent = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate().WithParent(parent);
        var child2 = EntityRegistry.Instance.Activate().WithParent(parent);
        Assert.Equal(2, parent.GetChildren().Count());
        
        // Act
        EntityRegistry.Instance.Deactivate(child1);
        
        // Assert
        var children = parent.GetChildren().ToList();
        Assert.Single(children);
        Assert.Contains(child2, children);
        Assert.DoesNotContain(child1, children);
    }

    [Fact]
    public void ResetEvent_ClearsSceneEntityHierarchy()
    {
        // Arrange
        var sceneParent = EntityRegistry.Instance.Activate();
        var sceneChild = EntityRegistry.Instance.Activate().WithParent(sceneParent);
        Assert.True(sceneChild.TryGetParent(out _));
        Assert.Single(sceneParent.GetChildren());
        
        // Act
        EventBus<ResetEvent>.Push(new());
        
        // Assert
        Assert.False(EntityRegistry.Instance.IsActive(sceneParent));
        Assert.False(EntityRegistry.Instance.IsActive(sceneChild));
    }

    [Fact]
    public void ResetEvent_PreservesLoadingEntityHierarchy()
    {
        // Arrange
        var loadingParent = EntityRegistry.Instance.ActivateLoading();
        var loadingChild = EntityRegistry.Instance.ActivateLoading().WithParent(loadingParent);
        Assert.True(loadingChild.TryGetParent(out var parent1));
        Assert.Equal(loadingParent.Index, parent1.Value.Index);
        
        // Act
        EventBus<ResetEvent>.Push(new());
        
        // Assert
        Assert.True(EntityRegistry.Instance.IsActive(loadingParent));
        Assert.True(EntityRegistry.Instance.IsActive(loadingChild));
        Assert.True(loadingChild.TryGetParent(out var parent2));
        Assert.Equal(loadingParent.Index, parent2.Value.Index);
        Assert.Single(loadingParent.GetChildren());
    }

    [Fact]
    public void ResetEvent_DoesNotPolluteHierarchy()
    {
        // Arrange
        var parent1 = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate().WithParent(parent1);
        Assert.True(child1.TryGetParent(out _));
        Assert.Single(parent1.GetChildren());
        
        // Act
        EventBus<ResetEvent>.Push(new());
        var parent2 = EntityRegistry.Instance.Activate();
        var child2 = EntityRegistry.Instance.Activate();
        Assert.Equal(parent1.Index, parent2.Index);
        Assert.Equal(child1.Index, child2.Index);
        Assert.False(child2.TryGetParent(out _));
        Assert.Empty(parent2.GetChildren());
        child2.WithParent(parent2);
        
        // Assert
        Assert.True(child2.TryGetParent(out var newParent));
        Assert.Equal(parent2.Index, newParent.Value.Index);
        Assert.Single(parent2.GetChildren());
    }

    [Fact]
    public void TryGetParent_ReturnsFalseWhenNoParent()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        
        // Act
        var hasParent = entity.TryGetParent(out var parent);
        
        // Assert
        Assert.False(hasParent);
        Assert.Null(parent);
    }

    [Fact]
    public void GetChildren_ReturnsEmptyWhenNoChildren()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        
        // Act
        var children = entity.GetChildren();
        
        // Assert
        Assert.Empty(children);
    }

    [Fact]
    public void NestedHierarchy_WorksCorrectly()
    {
        // Arrange
        var grandparent = EntityRegistry.Instance.Activate();
        var parent = EntityRegistry.Instance.Activate().WithParent(grandparent);
        var child = EntityRegistry.Instance.Activate().WithParent(parent);
        
        // Act
        child.TryGetParent(out var childParent);
        parent.TryGetParent(out var parentParent);
        var hasGrandparentParent = grandparent.TryGetParent(out _);
        var parentChildren = parent.GetChildren();
        var grandparentChildren = grandparent.GetChildren();
        
        // Assert
        Assert.NotNull(childParent);
        Assert.Equal(parent.Index, childParent.Value.Index);
        Assert.NotNull(parentParent);
        Assert.Equal(grandparent.Index, parentParent.Value.Index);
        Assert.False(hasGrandparentParent);
        Assert.Single(parentChildren);
        Assert.Single(grandparentChildren);
    }

    [Fact]
    public void WithParent_FiresBehaviorAddedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<BehaviorAddedEvent<Parent>>();
        var parent = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate();
        
        // Act
        child.WithParent(parent);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(child.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void RemoveParent_FiresBehaviorRemovedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<PostBehaviorRemovedEvent<Parent>>();
        var parent = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate().WithParent(parent);
        
        // Act
        BehaviorRegistry<Parent>.Instance.Remove(child);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(child.Index, listener.ReceivedEvents[0].Entity.Index);
    }
}
