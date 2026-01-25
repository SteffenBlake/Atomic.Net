using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Ids;

namespace Atomic.Net.MonoGame.Tests.BED.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class HierarchyRegistryIntegrationTests : IDisposable
{
    public HierarchyRegistryIntegrationTests()
    {
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both persistent and scene) between tests
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
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var parent));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child3", out var child3));
        
        var children = parent.Value.GetChildren().ToList();
        
        Assert.Equal(3, children.Count);
        Assert.Contains(child1.Value, children);
        Assert.Contains(child2.Value, children);
        Assert.Contains(child3.Value, children);
    }

    [Fact]
    public void RemoveParent_ClearsParentRelationship()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));
        Assert.True(child.Value.TryGetParent(out _));
        
        // Act
        BehaviorRegistry<ParentBehavior>.Instance.Remove(child.Value);
        
        // Assert
        Assert.False(child.Value.TryGetParent(out _));
    }

    [Fact]
    public void ParentDeactivation_OrphansChildren()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-three-children.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var parent));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));
        Assert.True(child1.Value.TryGetParent(out _));
        Assert.True(child2.Value.TryGetParent(out _));
        
        // Act
        EntityRegistry.Instance.Deactivate(parent.Value);
        
        // Assert
        Assert.False(child1.Value.TryGetParent(out _));
        Assert.False(child2.Value.TryGetParent(out _));
    }

    [Fact]
    public void ChildDeactivation_RemovesFromParentChildren()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-three-children.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var parent));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));
        Assert.Equal(3, parent.Value.GetChildren().Count());
        
        // Act
        EntityRegistry.Instance.Deactivate(child1.Value);
        
        // Assert
        var children = parent.Value.GetChildren().ToList();
        Assert.Equal(2, children.Count);
        Assert.Contains(child2.Value, children);
        Assert.DoesNotContain(child1.Value, children);
    }

    [Fact]
    public void ResetEvent_ClearsSceneEntityHierarchy()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var sceneParent));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var sceneChild));
        Assert.True(sceneChild.Value.TryGetParent(out _));
        Assert.Single(sceneParent.Value.GetChildren());
        
        // Act
        EventBus<ResetEvent>.Push(new());
        
        // Assert
        Assert.False(EntityRegistry.Instance.IsActive(sceneParent.Value));
        Assert.False(EntityRegistry.Instance.IsActive(sceneChild.Value));
    }

    [Fact]
    public void ResetEvent_PreservesPersistentEntityHierarchy()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadPersistentScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var persistentParent));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var persistentChild));
        Assert.True(persistentChild.Value.TryGetParent(out var parent1));
        Assert.Equal(persistentParent.Value.Index, parent1.Value.Index);
        
        // Act
        EventBus<ResetEvent>.Push(new());
        
        // Assert
        Assert.True(EntityRegistry.Instance.IsActive(persistentParent.Value));
        Assert.True(EntityRegistry.Instance.IsActive(persistentChild.Value));
        Assert.True(persistentChild.Value.TryGetParent(out var parent2));
        Assert.Equal(persistentParent.Value.Index, parent2.Value.Index);
        Assert.Single(persistentParent.Value.GetChildren());
    }

    [Fact]
    public void ResetEvent_DoesNotPolluteHierarchy()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var parent1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child1));
        Assert.True(child1.Value.TryGetParent(out _));
        Assert.Single(parent1.Value.GetChildren());
        
        // Act
        EventBus<ResetEvent>.Push(new());
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var parent2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child2));
        Assert.Equal(parent1.Value.Index, parent2.Value.Index);
        Assert.Equal(child1.Value.Index, child2.Value.Index);
        
        // Assert
        Assert.True(child2.Value.TryGetParent(out var newParent));
        Assert.Equal(parent2.Value.Index, newParent.Value.Index);
        Assert.Single(parent2.Value.GetChildren());
    }

    [Fact]
    public void TryGetParent_ReturnsFalseWhenNoParent()
    {
        // Arrange
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var entity));
        
        // Act
        var hasParent = entity.Value.TryGetParent(out var parent);
        
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
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var entity));
        
        // Act
        var children = entity.Value.GetChildren();
        
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
        Assert.True(EntityIdRegistry.Instance.TryResolve("grandparent", out var grandparent));
        Assert.True(EntityIdRegistry.Instance.TryResolve("parent", out var parent));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));
        
        child.Value.TryGetParent(out var childParent);
        parent.Value.TryGetParent(out var parentParent);
        var hasGrandparentParent = grandparent.Value.TryGetParent(out _);
        var parentChildren = parent.Value.GetChildren();
        var grandparentChildren = grandparent.Value.GetChildren();
        
        Assert.NotNull(childParent);
        Assert.Equal(parent.Value.Index, childParent.Value.Index);
        Assert.NotNull(parentParent);
        Assert.Equal(grandparent.Value.Index, parentParent.Value.Index);
        Assert.False(hasGrandparentParent);
        Assert.Single(parentChildren);
        Assert.Single(grandparentChildren);
    }

    [Fact]
    public void WithParent_FiresBehaviorAddedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<BehaviorAddedEvent<ParentBehavior>>();
        var scenePath = "BED/Fixtures/parent-child.json";
        
        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));
        
        // The event should have been fired during scene load
        Assert.NotEmpty(listener.ReceivedEvents);
        var childEvent = listener.ReceivedEvents.FirstOrDefault(e => e.Entity.Index == child.Value.Index);
        Assert.NotEqual(default, childEvent);
    }

    [Fact]
    public void RemoveParent_FiresBehaviorRemovedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<PostBehaviorRemovedEvent<ParentBehavior>>();
        var scenePath = "BED/Fixtures/parent-child.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));
        
        // Act
        BehaviorRegistry<ParentBehavior>.Instance.Remove(child.Value);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(child.Value.Index, listener.ReceivedEvents[0].Entity.Index);
    }
}
