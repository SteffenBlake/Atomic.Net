using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Ids;

namespace Atomic.Net.MonoGame.Tests.Core.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class EntityRegistryIntegrationTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    public EntityRegistryIntegrationTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both global and scene) between tests
        _errorLogger.Dispose();

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
        var isScene = entity.Value.Index.Visit(
            static _ => false,
            static _ => true,
            static () => false
        );
        Assert.True(isScene); // Should be scene partition (not global)
        Assert.True(EntityRegistry.Instance.IsActive(entity.Value));
        Assert.True(EntityRegistry.Instance.IsEnabled(entity.Value));

        Assert.True(entity.Value.Active);
        Assert.True(entity.Value.Enabled);
    }

    [Fact]
    public void LoadGlobalScene_AllocatesGlobalEntity()
    {
        // Arrange
        var scenePath = "Core/Fixtures/single-global-entity.json";

        // Act
        SceneLoader.Instance.LoadGlobalScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("global-entity", out var entity));
        
        // Assert
        var isGlobal = entity.Value.Index.Visit(
            static _ => true,
            static _ => false,
            static () => false
        );
        Assert.True(isGlobal); // Should be global partition
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
        // Scene root should be index 0 in the scene partition
        var sceneIndex = root.Index.Visit(
            static _ => throw new InvalidOperationException("Expected scene entity"),
            static scene => scene,
            static () => throw new InvalidOperationException()
        );
        Assert.Equal(0u, sceneIndex);
    }

    [Fact]
    public void GetGlobalRoot_ReturnsFirstGlobalEntity()
    {
        // Arrange

        // Act
        var root = EntityRegistry.Instance.GetGlobalRoot();
        
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
        
        // Extract scene indices and verify they're sequential
        var idx1 = entity1.Value.Index.Visit(
            static _ => throw new InvalidOperationException("Expected scene entity"),
            static scene => scene,
            static () => throw new InvalidOperationException()
        );
        var idx2 = entity2.Value.Index.Visit(
            static _ => throw new InvalidOperationException("Expected scene entity"),
            static scene => scene,
            static () => throw new InvalidOperationException()
        );
        var idx3 = entity3.Value.Index.Visit(
            static _ => throw new InvalidOperationException("Expected scene entity"),
            static scene => scene,
            static () => throw new InvalidOperationException()
        );
        
        Assert.Equal(idx1 + 1, idx2);
        Assert.Equal(idx2 + 1, idx3);
    }

    [Fact]
    public void LoadGlobalScene_AllocatesSequentialIndices()
    {
        // Arrange
        var scenePath = "Core/Fixtures/three-entities.json";

        // Act
        SceneLoader.Instance.LoadGlobalScene(scenePath);
        
        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-1", out var entity1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-2", out var entity2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-3", out var entity3));
        
        // Extract global indices and verify they're sequential
        var idx1 = entity1.Value.Index.Visit(
            static global => global,
            static _ => throw new InvalidOperationException("Expected global entity"),
            static () => throw new InvalidOperationException()
        );
        var idx2 = entity2.Value.Index.Visit(
            static global => global,
            static _ => throw new InvalidOperationException("Expected global entity"),
            static () => throw new InvalidOperationException()
        );
        var idx3 = entity3.Value.Index.Visit(
            static global => global,
            static _ => throw new InvalidOperationException("Expected global entity"),
            static () => throw new InvalidOperationException()
        );
        
        Assert.Equal(idx1 + 1, idx2);
        Assert.Equal(idx2 + 1, idx3);
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
        var globalPath = "Core/Fixtures/single-global-entity.json";
        var scenePath = "Core/Fixtures/single-scene-entity.json";
        
        SceneLoader.Instance.LoadGlobalScene(globalPath);
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("global-entity", out var globalEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("scene-entity", out var sceneEntity));
        
        Assert.True(EntityRegistry.Instance.IsActive(globalEntity.Value));
        Assert.True(EntityRegistry.Instance.IsActive(sceneEntity.Value));
        
        // Act
        EventBus<ResetEvent>.Push(new());
        
        // Assert
        Assert.True(EntityRegistry.Instance.IsActive(globalEntity.Value));
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
