using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Core;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class EntityRegistryIntegrationTests : IDisposable
{
    public EntityRegistryIntegrationTests()
    {
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Fire reset event to clean up scene entities between tests
        EventBus<ResetEvent>.Push(new());
    }

    [Fact]
    public void LoadGameScene_AllocatesSceneEntity()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity = EntityIdRegistry.Instance.TryResolve("scene-entity", out var resolvedEntity) ? resolvedEntity : default;
        
        // Assert
        Assert.True(entity.Index >= Constants.MaxLoadingEntities);
        Assert.True(entity.Index < Constants.MaxEntities);
        Assert.True(EntityRegistry.Instance.IsActive(entity));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));

        Assert.True(entity.Active);
        Assert.True(entity.Enabled);
    }

    [Fact]
    public void LoadLoadingScene_AllocatesLoadingEntity()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-loading-entity.json";

        // Act
        SceneLoader.Instance.LoadLoadingScene(scenePath);
        var entity = EntityIdRegistry.Instance.TryResolve("loading-entity", out var resolvedEntity) ? resolvedEntity : default;
        
        // Assert
        Assert.True(entity.Index < Constants.MaxLoadingEntities);
        Assert.True(EntityRegistry.Instance.IsActive(entity));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));
    }

    [Fact]
    public void GetSceneRoot_ReturnsFirstSceneEntity()
    {
        // Arrange

        // Act
        var root = EntityRegistry.Instance.GetSceneRoot();
        
        // Assert
        Assert.Equal(Constants.MaxLoadingEntities, root.Index);
    }

    [Fact]
    public void GetLoadingRoot_ReturnsFirstLoadingEntity()
    {
        // Arrange

        // Act
        var root = EntityRegistry.Instance.GetLoadingRoot();
        
        // Assert
        Assert.Equal((ushort)0, root.Index);
    }

    [Fact]
    public void LoadGameScene_AllocatesSequentialIndices()
    {
        // Arrange
        var scenePath = "Core/Fixtures/three-entities.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        var entity1Resolved = EntityIdRegistry.Instance.TryResolve("entity-1", out var entity1);
        var entity2Resolved = EntityIdRegistry.Instance.TryResolve("entity-2", out var entity2);
        var entity3Resolved = EntityIdRegistry.Instance.TryResolve("entity-3", out var entity3);
        
        Assert.True(entity1Resolved && entity2Resolved && entity3Resolved);
        Assert.Equal(entity1.Index + 1, entity2.Index);
        Assert.Equal(entity2.Index + 1, entity3.Index);
    }

    [Fact]
    public void LoadLoadingScene_AllocatesSequentialIndices()
    {
        // Arrange
        var scenePath = "Core/Fixtures/three-entities.json";

        // Act
        SceneLoader.Instance.LoadLoadingScene(scenePath);
        
        // Assert
        var entity1Resolved = EntityIdRegistry.Instance.TryResolve("entity-1", out var entity1);
        var entity2Resolved = EntityIdRegistry.Instance.TryResolve("entity-2", out var entity2);
        var entity3Resolved = EntityIdRegistry.Instance.TryResolve("entity-3", out var entity3);
        
        Assert.True(entity1Resolved && entity2Resolved && entity3Resolved);
        Assert.Equal(entity1.Index + 1, entity2.Index);
        Assert.Equal(entity2.Index + 1, entity3.Index);
    }

    [Fact]
    public void Deactivate_MarksEntityInactive()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity = EntityIdRegistry.Instance.TryResolve("scene-entity", out var resolvedEntity) ? resolvedEntity : default;
        Assert.True(EntityRegistry.Instance.IsActive(entity));
        
        // Act
        EntityRegistry.Instance.Deactivate(entity);
        
        // Assert
        Assert.False(EntityRegistry.Instance.IsActive(entity));
        Assert.False(EntityRegistry.Instance.IsEnabled(entity));
    }

    [Fact]
    public void Disable_MarksEntityDisabled()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity = EntityIdRegistry.Instance.TryResolve("scene-entity", out var resolvedEntity) ? resolvedEntity : default;
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));
        
        // Act
        EntityRegistry.Instance.Disable(entity);
        
        // Assert
        Assert.False(EntityRegistry.Instance.IsEnabled(entity));
        Assert.True(EntityRegistry.Instance.IsActive(entity));
    }

    [Fact]
    public void Enable_MarksEntityEnabled()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity = EntityIdRegistry.Instance.TryResolve("scene-entity", out var resolvedEntity) ? resolvedEntity : default;
        EntityRegistry.Instance.Disable(entity);
        Assert.False(EntityRegistry.Instance.IsEnabled(entity));
        
        // Act
        EntityRegistry.Instance.Enable(entity);
        
        // Assert
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));
    }

    [Fact]
    public void Deactivate_FiresEntityDeactivatedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<PostEntityDeactivatedEvent>();
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity = EntityIdRegistry.Instance.TryResolve("scene-entity", out var resolvedEntity) ? resolvedEntity : default;
        
        // Act
        EntityRegistry.Instance.Deactivate(entity);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Enable_FiresEntityEnabledEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<EntityEnabledEvent>();
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity = EntityIdRegistry.Instance.TryResolve("scene-entity", out var resolvedEntity) ? resolvedEntity : default;
        EntityRegistry.Instance.Disable(entity);
        listener.Clear();
        
        // Act
        EntityRegistry.Instance.Enable(entity);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Disable_FiresEntityDisabledEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<EntityDisabledEvent>();
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity = EntityIdRegistry.Instance.TryResolve("scene-entity", out var resolvedEntity) ? resolvedEntity : default;
        
        // Act
        EntityRegistry.Instance.Disable(entity);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void ResetEvent_DeactivatesOnlySceneEntities()
    {
        // Arrange
        var loadingPath = "Core/Fixtures/single-loading-entity.json";
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        
        SceneLoader.Instance.LoadLoadingScene(loadingPath);
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var loadingEntity = EntityIdRegistry.Instance.TryResolve("loading-entity", out var le) ? le : default;
        var sceneEntity = EntityIdRegistry.Instance.TryResolve("scene-entity", out var se) ? se : default;
        
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity));
        Assert.True(EntityRegistry.Instance.IsActive(sceneEntity));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        
        // Assert
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity));
        Assert.False(EntityRegistry.Instance.IsActive(sceneEntity));
    }

    [Fact]
    public void ResetEvent_AllowsSceneEntityReuse()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity1 = EntityIdRegistry.Instance.TryResolve("scene-entity", out var e1) ? e1 : default;
        var index1 = entity1.Index;
        
        // Act
        EventBus<ResetEvent>.Push(new());
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity2 = EntityIdRegistry.Instance.TryResolve("scene-entity", out var e2) ? e2 : default;
        
        // Assert
        Assert.Equal(index1, entity2.Index);
        Assert.True(EntityRegistry.Instance.IsActive(entity2));
    }

    [Fact]
    public void ResetEvent_DoesNotPollute_BasicCheck()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity1 = EntityIdRegistry.Instance.TryResolve("scene-entity", out var e1) ? e1 : default;
        var index1 = entity1.Index;
        Assert.True(EntityRegistry.Instance.IsActive(entity1));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        Assert.False(EntityRegistry.Instance.IsActive(entity1));
        SceneLoader.Instance.LoadGameScene(scenePath);
        var entity2 = EntityIdRegistry.Instance.TryResolve("scene-entity", out var e2) ? e2 : default;
        
        // Assert
        Assert.Equal(index1, entity2.Index);
        Assert.True(EntityRegistry.Instance.IsActive(entity2));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity2));
    }

    [Fact]
    public void GetActiveEntities_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var scenePath = "Core/Fixtures/three-entities.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var entity1 = EntityIdRegistry.Instance.TryResolve("entity-1", out var e1) ? e1 : default;
        var entity2 = EntityIdRegistry.Instance.TryResolve("entity-2", out var e2) ? e2 : default;
        var entity3 = EntityIdRegistry.Instance.TryResolve("entity-3", out var e3) ? e3 : default;
        
        EntityRegistry.Instance.Deactivate(entity2);
        
        // Act
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        
        // Assert
        Assert.Contains(entity1, activeEntities);
        Assert.DoesNotContain(entity2, activeEntities);
        Assert.Contains(entity3, activeEntities);
    }

    [Fact]
    public void GetEnabledEntities_ReturnsOnlyEnabledEntities()
    {
        // Arrange
        var scenePath = "Core/Fixtures/three-entities.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var entity1 = EntityIdRegistry.Instance.TryResolve("entity-1", out var e1) ? e1 : default;
        var entity2 = EntityIdRegistry.Instance.TryResolve("entity-2", out var e2) ? e2 : default;
        var entity3 = EntityIdRegistry.Instance.TryResolve("entity-3", out var e3) ? e3 : default;
        
        EntityRegistry.Instance.Disable(entity2);
        
        // Act
        var enabledEntities = EntityRegistry.Instance.GetEnabledEntities().ToList();
        
        // Assert
        Assert.Contains(entity1, enabledEntities);
        Assert.DoesNotContain(entity2, enabledEntities);
        Assert.Contains(entity3, enabledEntities);
    }
}
