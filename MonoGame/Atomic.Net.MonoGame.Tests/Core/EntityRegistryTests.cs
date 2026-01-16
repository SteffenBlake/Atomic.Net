using Xunit;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.Core;

public sealed class EntityRegistryTests : IDisposable
{
    public EntityRegistryTests()
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
    public void Activate_AllocatesSceneEntity()
    {
        // Arrange

        // Act
        var entity = EntityRegistry.Instance.Activate();
        
        // Assert
        Assert.True(entity.Index >= Constants.MaxLoadingEntities);
        Assert.True(entity.Index < Constants.MaxEntities);
        Assert.True(EntityRegistry.Instance.IsActive(entity));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));
    }

    [Fact]
    public void ActivateLoading_AllocatesLoadingEntity()
    {
        // Arrange

        // Act
        var entity = EntityRegistry.Instance.ActivateLoading();
        
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
    public void Activate_AllocatesSequentialIndices()
    {
        // Arrange

        // Act
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        var entity3 = EntityRegistry.Instance.Activate();
        
        // Assert
        Assert.Equal(entity1.Index + 1, entity2.Index);
        Assert.Equal(entity2.Index + 1, entity3.Index);
    }

    [Fact]
    public void ActivateLoading_AllocatesSequentialIndices()
    {
        // Arrange

        // Act
        var entity1 = EntityRegistry.Instance.ActivateLoading();
        var entity2 = EntityRegistry.Instance.ActivateLoading();
        var entity3 = EntityRegistry.Instance.ActivateLoading();
        
        // Assert
        Assert.Equal(entity1.Index + 1, entity2.Index);
        Assert.Equal(entity2.Index + 1, entity3.Index);
    }

    [Fact]
    public void Deactivate_MarksEntityInactive()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
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
        var entity = EntityRegistry.Instance.Activate();
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
        var entity = EntityRegistry.Instance.Activate();
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
        using var listener = new FakeEventListener<EntityDeactivatedEvent>();
        var entity = EntityRegistry.Instance.Activate();
        
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
        var entity = EntityRegistry.Instance.Activate();
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
        var entity = EntityRegistry.Instance.Activate();
        
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
        var loadingEntity1 = EntityRegistry.Instance.ActivateLoading();
        var loadingEntity2 = EntityRegistry.Instance.ActivateLoading();
        var sceneEntity1 = EntityRegistry.Instance.Activate();
        var sceneEntity2 = EntityRegistry.Instance.Activate();
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity1));
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity2));
        Assert.True(EntityRegistry.Instance.IsActive(sceneEntity1));
        Assert.True(EntityRegistry.Instance.IsActive(sceneEntity2));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        
        // Assert
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity1));
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity2));
        Assert.False(EntityRegistry.Instance.IsActive(sceneEntity1));
        Assert.False(EntityRegistry.Instance.IsActive(sceneEntity2));
    }

    [Fact]
    public void ResetEvent_AllowsSceneEntityReuse()
    {
        // Arrange
        var entity1 = EntityRegistry.Instance.Activate();
        var index1 = entity1.Index;
        
        // Act
        EventBus<ResetEvent>.Push(new());
        var entity2 = EntityRegistry.Instance.Activate();
        
        // Assert
        Assert.Equal(index1, entity2.Index);
        Assert.True(EntityRegistry.Instance.IsActive(entity2));
    }

    [Fact]
    public void ResetEvent_DoesNotPollute_BasicCheck()
    {
        // Arrange
        var entity1 = EntityRegistry.Instance.Activate();
        var index1 = entity1.Index;
        Assert.True(EntityRegistry.Instance.IsActive(entity1));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        Assert.False(EntityRegistry.Instance.IsActive(entity1));
        var entity2 = EntityRegistry.Instance.Activate();
        
        // Assert
        Assert.Equal(index1, entity2.Index);
        Assert.True(EntityRegistry.Instance.IsActive(entity2));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity2));
    }

    [Fact]
    public void GetActiveEntities_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        var entity3 = EntityRegistry.Instance.Activate();
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
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        var entity3 = EntityRegistry.Instance.Activate();
        EntityRegistry.Instance.Disable(entity2);
        
        // Act
        var enabledEntities = EntityRegistry.Instance.GetEnabledEntities().ToList();
        
        // Assert
        Assert.Contains(entity1, enabledEntities);
        Assert.DoesNotContain(entity2, enabledEntities);
        Assert.Contains(entity3, enabledEntities);
    }
}
