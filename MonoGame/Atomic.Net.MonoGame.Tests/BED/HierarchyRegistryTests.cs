using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;

namespace Atomic.Net.MonoGame.Tests.BED;

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
        var parent = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate();
        
        child.WithParent(parent);
        
        Assert.True(child.TryGetParent(out var retrievedParent));
        Assert.Equal(parent.Index, retrievedParent!.Value.Index);
    }

    [Fact]
    public void GetChildren_ReturnsChildEntities()
    {
        var parent = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate().WithParent(parent);
        var child2 = EntityRegistry.Instance.Activate().WithParent(parent);
        var child3 = EntityRegistry.Instance.Activate().WithParent(parent);
        
        var children = parent.GetChildren().ToList();
        
        Assert.Equal(3, children.Count);
        Assert.Contains(child1, children);
        Assert.Contains(child2, children);
        Assert.Contains(child3, children);
    }

    [Fact]
    public void RemoveParent_ClearsParentRelationship()
    {
        var parent = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate().WithParent(parent);
        
        Assert.True(child.TryGetParent(out _));
        
        BehaviorRegistry<Parent>.Instance.Remove(child);
        
        Assert.False(child.TryGetParent(out _));
    }

    [Fact]
    public void ParentDeactivation_OrphansChildren()
    {
        var parent = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate().WithParent(parent);
        var child2 = EntityRegistry.Instance.Activate().WithParent(parent);
        
        Assert.True(child1.TryGetParent(out _));
        Assert.True(child2.TryGetParent(out _));
        
        EntityRegistry.Instance.Deactivate(parent);
        
        // Children should be orphaned
        Assert.False(child1.TryGetParent(out _));
        Assert.False(child2.TryGetParent(out _));
    }

    [Fact]
    public void ChildDeactivation_RemovesFromParentChildren()
    {
        var parent = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate().WithParent(parent);
        var child2 = EntityRegistry.Instance.Activate().WithParent(parent);
        
        Assert.Equal(2, parent.GetChildren().Count());
        
        EntityRegistry.Instance.Deactivate(child1);
        
        var children = parent.GetChildren().ToList();
        Assert.Single(children);
        Assert.Contains(child2, children);
        Assert.DoesNotContain(child1, children);
    }

    [Fact]
    public void ResetEvent_ClearsSceneEntityHierarchy()
    {
        var sceneParent = EntityRegistry.Instance.Activate();
        var sceneChild = EntityRegistry.Instance.Activate().WithParent(sceneParent);
        
        Assert.True(sceneChild.TryGetParent(out _));
        Assert.Single(sceneParent.GetChildren());
        
        EventBus<ResetEvent>.Push(new());
        
        // Both should be deactivated, hierarchy should be cleared
        Assert.False(EntityRegistry.Instance.IsActive(sceneParent));
        Assert.False(EntityRegistry.Instance.IsActive(sceneChild));
    }

    [Fact]
    public void ResetEvent_PreservesLoadingEntityHierarchy()
    {
        var loadingParent = EntityRegistry.Instance.ActivateLoading();
        var loadingChild = EntityRegistry.Instance.ActivateLoading().WithParent(loadingParent);
        
        Assert.True(loadingChild.TryGetParent(out var parent1));
        Assert.Equal(loadingParent.Index, parent1!.Value.Index);
        
        EventBus<ResetEvent>.Push(new());
        
        // Loading entities should still be active with hierarchy intact
        Assert.True(EntityRegistry.Instance.IsActive(loadingParent));
        Assert.True(EntityRegistry.Instance.IsActive(loadingChild));
        Assert.True(loadingChild.TryGetParent(out var parent2));
        Assert.Equal(loadingParent.Index, parent2!.Value.Index);
        Assert.Single(loadingParent.GetChildren());
    }

    [Fact]
    public void ResetEvent_DoesNotPolluteHierarchy()
    {
        // Create parent-child relationship
        var parent1 = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate().WithParent(parent1);
        
        Assert.True(child1.TryGetParent(out _));
        Assert.Single(parent1.GetChildren());
        
        // Reset
        EventBus<ResetEvent>.Push(new());
        
        // Create new entities at same indices
        var parent2 = EntityRegistry.Instance.Activate();
        var child2 = EntityRegistry.Instance.Activate();
        
        Assert.Equal(parent1.Index, parent2.Index);
        Assert.Equal(child1.Index, child2.Index);
        
        // Should not have parent relationship from previous entities
        Assert.False(child2.TryGetParent(out _));
        Assert.Empty(parent2.GetChildren());
        
        // Can establish new relationship cleanly
        child2.WithParent(parent2);
        Assert.True(child2.TryGetParent(out var newParent));
        Assert.Equal(parent2.Index, newParent!.Value.Index);
        Assert.Single(parent2.GetChildren());
    }

    [Fact]
    public void TryGetParent_ReturnsFalseWhenNoParent()
    {
        var entity = EntityRegistry.Instance.Activate();
        
        var hasParent = entity.TryGetParent(out var parent);
        
        Assert.False(hasParent);
        Assert.Null(parent);
    }

    [Fact]
    public void GetChildren_ReturnsEmptyWhenNoChildren()
    {
        var entity = EntityRegistry.Instance.Activate();
        
        var children = entity.GetChildren();
        
        Assert.Empty(children);
    }

    [Fact]
    public void NestedHierarchy_WorksCorrectly()
    {
        var grandparent = EntityRegistry.Instance.Activate();
        var parent = EntityRegistry.Instance.Activate().WithParent(grandparent);
        var child = EntityRegistry.Instance.Activate().WithParent(parent);
        
        // Verify full hierarchy
        Assert.True(child.TryGetParent(out var childParent));
        Assert.Equal(parent.Index, childParent!.Value.Index);
        
        Assert.True(parent.TryGetParent(out var parentParent));
        Assert.Equal(grandparent.Index, parentParent!.Value.Index);
        
        Assert.False(grandparent.TryGetParent(out _));
        
        Assert.Single(parent.GetChildren());
        Assert.Single(grandparent.GetChildren());
    }

    [Fact]
    public void WithParent_FiresBehaviorAddedEvent()
    {
        var listener = new EventListener<BehaviorAddedEvent<Parent>>();
        EventBus<BehaviorAddedEvent<Parent>>.Register(listener);
        
        var parent = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate();
        
        child.WithParent(parent);
        
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(child.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void RemoveParent_FiresBehaviorRemovedEvent()
    {
        var parent = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate().WithParent(parent);
        
        var listener = new EventListener<BehaviorRemovedEvent<Parent>>();
        EventBus<BehaviorRemovedEvent<Parent>>.Register(listener);
        
        BehaviorRegistry<Parent>.Instance.Remove(child);
        
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(child.Index, listener.ReceivedEvents[0].Entity.Index);
    }
}
