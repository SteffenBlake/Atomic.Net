using Xunit;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Core.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class EntityRegistryIntegrationTests : IDisposable
{
    public EntityRegistryIntegrationTests()
    {
        AtomicSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both persistent and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void LoadGameScene_AllocatesSceneEntity()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity));
        
        // Assert
        Assert.True(entity.Value.Index >= Constants.MaxPersistentEntities);
        Assert.True(entity.Value.Index < Constants.MaxEntities);
        Assert.True(EntityRegistry.Instance.IsActive(entity.Value));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity.Value));

        Assert.True(entity.Value.Active);
        Assert.True(entity.Value.Enabled);
    }

    [Fact]
    public void LoadPersistentScene_AllocatesPersistentEntity()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-persistent-entity.json";

        // Act
        SceneLoader.Instance.LoadPersistentScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-entity", out var entity));
        
        // Assert
        Assert.True(entity.Value.Index < Constants.MaxPersistentEntities);
        Assert.True(EntityRegistry.Instance.IsActive(entity.Value));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity.Value));
    }

    [Fact]
    public void GetSceneRoot_ReturnsFirstSceneEntity()
    {
        // Arrange

        // Act
        var root = EntityRegistry.Instance.GetSceneRoot();
        
        // Assert
        Assert.Equal(Constants.MaxPersistentEntities, root.Index);
    }

    [Fact]
    public void GetPersistentRoot_ReturnsFirstPersistentEntity()
    {
        // Arrange

        // Act
        var root = EntityRegistry.Instance.GetPersistentRoot();
        
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
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-1", out var entity1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-2", out var entity2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-3", out var entity3));
        
        Assert.Equal(entity1.Value.Index + 1, entity2.Value.Index);
        Assert.Equal(entity2.Value.Index + 1, entity3.Value.Index);
    }

    [Fact]
    public void LoadPersistentScene_AllocatesSequentialIndices()
    {
        // Arrange
        var scenePath = "Core/Fixtures/three-entities.json";

        // Act
        SceneLoader.Instance.LoadPersistentScene(scenePath);
        
        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-1", out var entity1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-2", out var entity2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-3", out var entity3));
        
        Assert.Equal(entity1.Value.Index + 1, entity2.Value.Index);
        Assert.Equal(entity2.Value.Index + 1, entity3.Value.Index);
    }

    [Fact]
    public void Deactivate_MarksEntityInactive()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity));
        Assert.True(EntityRegistry.Instance.IsActive(entity.Value));
        
        // Act
        EntityRegistry.Instance.Deactivate(entity.Value);
        
        // Assert
        Assert.False(EntityRegistry.Instance.IsActive(entity.Value));
        Assert.False(EntityRegistry.Instance.IsEnabled(entity.Value));
    }

    [Fact]
    public void Disable_MarksEntityDisabled()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity.Value));
        
        // Act
        EntityRegistry.Instance.Disable(entity.Value);
        
        // Assert
        Assert.False(EntityRegistry.Instance.IsEnabled(entity.Value));
        Assert.True(EntityRegistry.Instance.IsActive(entity.Value));
    }

    [Fact]
    public void Enable_MarksEntityEnabled()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity));
        EntityRegistry.Instance.Disable(entity.Value);
        Assert.False(EntityRegistry.Instance.IsEnabled(entity.Value));
        
        // Act
        EntityRegistry.Instance.Enable(entity.Value);
        
        // Assert
        Assert.True(EntityRegistry.Instance.IsEnabled(entity.Value));
    }

    [Fact]
    public void Deactivate_FiresEntityDeactivatedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<PostEntityDeactivatedEvent>();
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity));
        
        // Act
        EntityRegistry.Instance.Deactivate(entity.Value);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Value.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Enable_FiresEntityEnabledEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<EntityEnabledEvent>();
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity));
        EntityRegistry.Instance.Disable(entity.Value);
        listener.Clear();
        
        // Act
        EntityRegistry.Instance.Enable(entity.Value);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Value.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Disable_FiresEntityDisabledEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<EntityDisabledEvent>();
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity));
        
        // Act
        EntityRegistry.Instance.Disable(entity.Value);
        
        // Assert
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Value.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void ResetEvent_DeactivatesOnlySceneEntities()
    {
        // Arrange
        var persistentPath = "Core/Fixtures/single-persistent-entity.json";
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        
        SceneLoader.Instance.LoadPersistentScene(persistentPath);
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-entity", out var persistentEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var sceneEntity));
        
        Assert.True(EntityRegistry.Instance.IsActive(persistentEntity.Value));
        Assert.True(EntityRegistry.Instance.IsActive(sceneEntity.Value));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        
        // Assert
        Assert.True(EntityRegistry.Instance.IsActive(persistentEntity.Value));
        Assert.False(EntityRegistry.Instance.IsActive(sceneEntity.Value));
    }

    [Fact]
    public void ResetEvent_AllowsSceneEntityReuse()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity1));
        var index1 = entity1.Value.Index;
        
        // Act
        EventBus<ResetEvent>.Push(new());
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity2));
        
        // Assert
        Assert.Equal(index1, entity2.Value.Index);
        Assert.True(EntityRegistry.Instance.IsActive(entity2.Value));
    }

    [Fact]
    public void ResetEvent_DoesNotPollute_BasicCheck()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity1));
        var index1 = entity1.Value.Index;
        Assert.True(EntityRegistry.Instance.IsActive(entity1.Value));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        Assert.False(EntityRegistry.Instance.IsActive(entity1.Value));
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var entity2));
        
        // Assert
        Assert.Equal(index1, entity2.Value.Index);
        Assert.True(EntityRegistry.Instance.IsActive(entity2.Value));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity2.Value));
    }

    [Fact]
    public void GetActiveEntities_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var scenePath = "Core/Fixtures/three-entities.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-1", out var entity1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-2", out var entity2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-3", out var entity3));
        
        EntityRegistry.Instance.Deactivate(entity2.Value);
        
        // Act
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        
        // Assert
        Assert.Contains(entity1.Value, activeEntities);
        Assert.DoesNotContain(entity2.Value, activeEntities);
        Assert.Contains(entity3.Value, activeEntities);
    }

    [Fact]
    public void GetEnabledEntities_ReturnsOnlyEnabledEntities()
    {
        // Arrange
        var scenePath = "Core/Fixtures/three-entities.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-1", out var entity1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-2", out var entity2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-3", out var entity3));
        
        EntityRegistry.Instance.Disable(entity2.Value);
        
        // Act
        var enabledEntities = EntityRegistry.Instance.GetEnabledEntities().ToList();
        
        // Assert
        Assert.Contains(entity1.Value, enabledEntities);
        Assert.DoesNotContain(entity2.Value, enabledEntities);
        Assert.Contains(entity3.Value, enabledEntities);
    }
}
