using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.BED.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class HierarchyRegistryIntegrationTests : IDisposable
{
    public HierarchyRegistryIntegrationTests()
    {
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both loading and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void LoadScene_WithParent_SetsParentChildRelationship()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        
        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var parent));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));
        
        Assert.True(child.Value.TryGetParent(out var retrievedParent));
        Assert.Equal(parent.Value.Index, retrievedParent.Value.Index);
    }

    [Fact]
    public void LoadScene_WithMultipleChildren_ReturnsChildEntities()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-three-children.json";
        
        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        var parentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent);
        var child1Resolved = EntityIdRegistry.Instance.TryResolve("child1", out var child1);
        var child2Resolved = EntityIdRegistry.Instance.TryResolve("child2", out var child2);
        var child3Resolved = EntityIdRegistry.Instance.TryResolve("child3", out var child3);
        Assert.True(parentResolved && child1Resolved && child2Resolved && child3Resolved);
        
        var children = parent.GetChildren().ToList();
        
        Assert.Equal(3, children.Count);
        Assert.Contains(child1, children);
        Assert.Contains(child2, children);
        Assert.Contains(child3, children);
    }

    [Fact]
    public void RemoveParent_ClearsParentRelationship()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var childResolved = EntityIdRegistry.Instance.TryResolve("child", out var child);
        Assert.True(childResolved);
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
        var scenePath = "BED/Fixtures/parent-three-children.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var parentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent);
        var child1Resolved = EntityIdRegistry.Instance.TryResolve("child1", out var child1);
        var child2Resolved = EntityIdRegistry.Instance.TryResolve("child2", out var child2);
        Assert.True(parentResolved && child1Resolved && child2Resolved);
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
        var scenePath = "BED/Fixtures/parent-three-children.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var parentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent);
        var child1Resolved = EntityIdRegistry.Instance.TryResolve("child1", out var child1);
        var child2Resolved = EntityIdRegistry.Instance.TryResolve("child2", out var child2);
        Assert.True(parentResolved && child1Resolved && child2Resolved);
        Assert.Equal(3, parent.GetChildren().Count());
        
        // Act
        EntityRegistry.Instance.Deactivate(child1);
        
        // Assert
        var children = parent.GetChildren().ToList();
        Assert.Equal(2, children.Count);
        Assert.Contains(child2, children);
        Assert.DoesNotContain(child1, children);
    }

    [Fact]
    public void ResetEvent_ClearsSceneEntityHierarchy()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var sceneParentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var sceneParent);
        var sceneChildResolved = EntityIdRegistry.Instance.TryResolve("child", out var sceneChild);
        Assert.True(sceneParentResolved && sceneChildResolved);
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
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadLoadingScene(scenePath);
        
        var loadingParentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var loadingParent);
        var loadingChildResolved = EntityIdRegistry.Instance.TryResolve("child", out var loadingChild);
        Assert.True(loadingParentResolved && loadingChildResolved);
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
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var parent1Resolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent1);
        var child1Resolved = EntityIdRegistry.Instance.TryResolve("child", out var child1);
        Assert.True(parent1Resolved && child1Resolved);
        Assert.True(child1.TryGetParent(out _));
        Assert.Single(parent1.GetChildren());
        
        // Act
        EventBus<ResetEvent>.Push(new());
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var parent2Resolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent2);
        var child2Resolved = EntityIdRegistry.Instance.TryResolve("child", out var child2);
        Assert.True(parent2Resolved && child2Resolved);
        Assert.Equal(parent1.Index, parent2.Index);
        Assert.Equal(child1.Index, child2.Index);
        
        // Assert
        Assert.True(child2.TryGetParent(out var newParent));
        Assert.Equal(parent2.Index, newParent.Value.Index);
        Assert.Single(parent2.GetChildren());
    }

    [Fact]
    public void TryGetParent_ReturnsFalseWhenNoParent()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var parentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var entity);
        Assert.True(parentResolved);
        
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
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var childResolved = EntityIdRegistry.Instance.TryResolve("child", out var entity);
        Assert.True(childResolved);
        
        // Act
        var children = entity.GetChildren();
        
        // Assert
        Assert.Empty(children);
    }

    [Fact]
    public void NestedHierarchy_WorksCorrectly()
    {
        // Arrange
        var scenePath = "BED/Fixtures/nested-hierarchy.json";
        
        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        var grandparentResolved = EntityIdRegistry.Instance.TryResolve("grandparent", out var grandparent);
        var parentResolved = EntityIdRegistry.Instance.TryResolve("parent", out var parent);
        var childResolved = EntityIdRegistry.Instance.TryResolve("child", out var child);
        Assert.True(grandparentResolved && parentResolved && childResolved);
        
        child.TryGetParent(out var childParent);
        parent.TryGetParent(out var parentParent);
        var hasGrandparentParent = grandparent.TryGetParent(out _);
        var parentChildren = parent.GetChildren();
        var grandparentChildren = grandparent.GetChildren();
        
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
        var scenePath = "BED/Fixtures/parent-child.json";
        
        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        var childResolved = EntityIdRegistry.Instance.TryResolve("child", out var child);
        Assert.True(childResolved);
        
        // The event should have been fired during scene load
        Assert.NotEmpty(listener.ReceivedEvents);
        var childEvent = listener.ReceivedEvents.FirstOrDefault(e => e.Entity.Index == child.Index);
        Assert.NotEqual(default, childEvent);
    }

    [Fact]
    public void RemoveParent_FiresBehaviorRemovedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<PostBehaviorRemovedEvent<Parent>>();
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var childResolved = EntityIdRegistry.Instance.TryResolve("child", out var child);
        Assert.True(childResolved);
        
        // Act
        BehaviorRegistry<Parent>.Instance.Remove(child);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(child.Index, listener.ReceivedEvents[0].Entity.Index);
    }
}
