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
        var entity = EntityRegistry.Instance.Activate();
        
        // Should allocate from scene partition (>= MaxLoadingEntities)
        Assert.True(entity.Index >= Constants.MaxLoadingEntities);
        Assert.True(entity.Index < Constants.MaxEntities);
        Assert.True(EntityRegistry.Instance.IsActive(entity));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));
    }

    [Fact]
    public void ActivateLoading_AllocatesLoadingEntity()
    {
        var entity = EntityRegistry.Instance.ActivateLoading();
        
        // Should allocate from loading partition (< MaxLoadingEntities)
        Assert.True(entity.Index < Constants.MaxLoadingEntities);
        Assert.True(EntityRegistry.Instance.IsActive(entity));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));
    }

    [Fact]
    public void GetSceneRoot_ReturnsFirstSceneEntity()
    {
        var root = EntityRegistry.Instance.GetSceneRoot();
        
        Assert.Equal(Constants.MaxLoadingEntities, root.Index);
    }

    [Fact]
    public void GetLoadingRoot_ReturnsFirstLoadingEntity()
    {
        var root = EntityRegistry.Instance.GetLoadingRoot();
        
        Assert.Equal((ushort)0, root.Index);
    }

    [Fact]
    public void Activate_AllocatesSequentialIndices()
    {
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        var entity3 = EntityRegistry.Instance.Activate();
        
        // Should allocate sequential indices in scene partition
        Assert.Equal(entity1.Index + 1, entity2.Index);
        Assert.Equal(entity2.Index + 1, entity3.Index);
    }

    [Fact]
    public void ActivateLoading_AllocatesSequentialIndices()
    {
        var entity1 = EntityRegistry.Instance.ActivateLoading();
        var entity2 = EntityRegistry.Instance.ActivateLoading();
        var entity3 = EntityRegistry.Instance.ActivateLoading();
        
        // Should allocate sequential indices in loading partition
        Assert.Equal(entity1.Index + 1, entity2.Index);
        Assert.Equal(entity2.Index + 1, entity3.Index);
    }

    [Fact]
    public void Deactivate_MarksEntityInactive()
    {
        var entity = EntityRegistry.Instance.Activate();
        Assert.True(EntityRegistry.Instance.IsActive(entity));
        
        EntityRegistry.Instance.Deactivate(entity);
        
        Assert.False(EntityRegistry.Instance.IsActive(entity));
        Assert.False(EntityRegistry.Instance.IsEnabled(entity));
    }

    [Fact]
    public void Disable_MarksEntityDisabled()
    {
        var entity = EntityRegistry.Instance.Activate();
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));
        
        EntityRegistry.Instance.Disable(entity);
        
        Assert.False(EntityRegistry.Instance.IsEnabled(entity));
        Assert.True(EntityRegistry.Instance.IsActive(entity)); // Still active, just disabled
    }

    [Fact]
    public void Enable_MarksEntityEnabled()
    {
        var entity = EntityRegistry.Instance.Activate();
        EntityRegistry.Instance.Disable(entity);
        Assert.False(EntityRegistry.Instance.IsEnabled(entity));
        
        EntityRegistry.Instance.Enable(entity);
        
        Assert.True(EntityRegistry.Instance.IsEnabled(entity));
    }

    [Fact]
    public void Deactivate_FiresEntityDeactivatedEvent()
    {
        var listener = new EventListener<EntityDeactivatedEvent>();
        EventBus<EntityDeactivatedEvent>.Register(listener);
        
        var entity = EntityRegistry.Instance.Activate();
        EntityRegistry.Instance.Deactivate(entity);
        
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Enable_FiresEntityEnabledEvent()
    {
        var listener = new EventListener<EntityEnabledEvent>();
        EventBus<EntityEnabledEvent>.Register(listener);
        
        var entity = EntityRegistry.Instance.Activate();
        EntityRegistry.Instance.Disable(entity);
        
        listener.Clear(); // Clear events from initial activation
        
        EntityRegistry.Instance.Enable(entity);
        
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void Disable_FiresEntityDisabledEvent()
    {
        var listener = new EventListener<EntityDisabledEvent>();
        EventBus<EntityDisabledEvent>.Register(listener);
        
        var entity = EntityRegistry.Instance.Activate();
        EntityRegistry.Instance.Disable(entity);
        
        Assert.Single(listener.ReceivedEvents);
        Assert.Equal(entity.Index, listener.ReceivedEvents[0].Entity.Index);
    }

    [Fact]
    public void ResetEvent_DeactivatesOnlySceneEntities()
    {
        // Create loading and scene entities
        var loadingEntity1 = EntityRegistry.Instance.ActivateLoading();
        var loadingEntity2 = EntityRegistry.Instance.ActivateLoading();
        var sceneEntity1 = EntityRegistry.Instance.Activate();
        var sceneEntity2 = EntityRegistry.Instance.Activate();
        
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity1));
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity2));
        Assert.True(EntityRegistry.Instance.IsActive(sceneEntity1));
        Assert.True(EntityRegistry.Instance.IsActive(sceneEntity2));
        
        // Fire reset event
        EventBus<ResetEvent>.Push(new());
        
        // Loading entities should still be active
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity1));
        Assert.True(EntityRegistry.Instance.IsActive(loadingEntity2));
        
        // Scene entities should be deactivated
        Assert.False(EntityRegistry.Instance.IsActive(sceneEntity1));
        Assert.False(EntityRegistry.Instance.IsActive(sceneEntity2));
    }

    [Fact]
    public void ResetEvent_AllowsSceneEntityReuse()
    {
        // Create scene entity
        var entity1 = EntityRegistry.Instance.Activate();
        var index1 = entity1.Index;
        
        // Fire reset
        EventBus<ResetEvent>.Push(new());
        
        // Create another scene entity - should get same index
        var entity2 = EntityRegistry.Instance.Activate();
        
        Assert.Equal(index1, entity2.Index);
        Assert.True(EntityRegistry.Instance.IsActive(entity2));
    }

    [Fact]
    public void ResetEvent_DoesNotPollute_BasicCheck()
    {
        // Create and activate first entity
        var entity1 = EntityRegistry.Instance.Activate();
        var index1 = entity1.Index;
        Assert.True(EntityRegistry.Instance.IsActive(entity1));
        
        // Reset
        EventBus<ResetEvent>.Push(new());
        Assert.False(EntityRegistry.Instance.IsActive(entity1));
        
        // Create second entity at same index
        var entity2 = EntityRegistry.Instance.Activate();
        Assert.Equal(index1, entity2.Index);
        Assert.True(EntityRegistry.Instance.IsActive(entity2));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity2));
    }

    [Fact]
    public void GetActiveEntities_ReturnsOnlyActiveEntities()
    {
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        var entity3 = EntityRegistry.Instance.Activate();
        
        EntityRegistry.Instance.Deactivate(entity2);
        
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        
        Assert.Contains(entity1, activeEntities);
        Assert.DoesNotContain(entity2, activeEntities);
        Assert.Contains(entity3, activeEntities);
    }

    [Fact]
    public void GetEnabledEntities_ReturnsOnlyEnabledEntities()
    {
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        var entity3 = EntityRegistry.Instance.Activate();
        
        EntityRegistry.Instance.Disable(entity2);
        
        var enabledEntities = EntityRegistry.Instance.GetEnabledEntities().ToList();
        
        Assert.Contains(entity1, enabledEntities);
        Assert.DoesNotContain(entity2, enabledEntities);
        Assert.Contains(entity3, enabledEntities);
    }
}
