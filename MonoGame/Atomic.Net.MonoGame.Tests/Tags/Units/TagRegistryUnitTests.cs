using Atomic.Net.MonoGame;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Tags;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Tags.Units;

[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class TagRegistryUnitTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    
    public TagRegistryUnitTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        EventBus<ShutdownEvent>.Push(new());
        _errorLogger.Dispose();
    }

    #region Positive Tests

    [Fact]
    public void TagRegistry_RegisterEntityWithSingleTag_ResolvesCorrectly()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();

        // Act
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );

        // Assert
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var entities));
        Assert.True(entities.HasValue(entity.Index));
    }

    [Fact]
    public void TagRegistry_RegisterEntityWithMultipleTags_ResolvesAllTags()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();

        // Act
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy").Add("goblin").Add("hostile") }
        );

        // Assert
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var enemyEntities));
        Assert.True(enemyEntities.HasValue(entity.Index));

        Assert.True(TagRegistry.Instance.TryResolve("goblin", out var goblinEntities));
        Assert.True(goblinEntities.HasValue(entity.Index));

        Assert.True(TagRegistry.Instance.TryResolve("hostile", out var hostileEntities));
        Assert.True(hostileEntities.HasValue(entity.Index));
    }

    [Fact]
    public void TagRegistry_RegisterMultipleEntitiesWithSameTag_ResolvesAllEntities()
    {
        // Arrange
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        var entity3 = EntityRegistry.Instance.Activate();

        // Act
        entity1.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );
        entity2.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );
        entity3.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );

        // Assert
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var entities));
        Assert.True(entities.HasValue(entity1.Index));
        Assert.True(entities.HasValue(entity2.Index));
        Assert.True(entities.HasValue(entity3.Index));
    }

    [Fact]
    public void TagRegistry_AddTagsToExistingEntity_BothOldAndNewTagsResolve()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );

        // Act - Update with additional tags
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy").Add("boss") }
        );

        // Assert
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var enemyEntities));
        Assert.True(enemyEntities.HasValue(entity.Index));

        Assert.True(TagRegistry.Instance.TryResolve("boss", out var bossEntities));
        Assert.True(bossEntities.HasValue(entity.Index));
    }

    [Fact]
    public void TagRegistry_CaseInsensitiveMatching_ResolvesTagsRegardlessOfCase()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );

        // Act & Assert - All case variations should resolve to same entity
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var entities1));
        Assert.True(entities1.HasValue(entity.Index));

        Assert.True(TagRegistry.Instance.TryResolve("Enemy", out var entities2));
        Assert.True(entities2.HasValue(entity.Index));

        Assert.True(TagRegistry.Instance.TryResolve("ENEMY", out var entities3));
        Assert.True(entities3.HasValue(entity.Index));
    }

    [Fact]
    public void TagRegistry_EntityWithNoTags_NoEntriesInRegistry()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();

        // Act - Entity has no TagsBehavior

        // Assert
        Assert.False(TagRegistry.Instance.TryResolve("enemy", out _));
    }

    #endregion

    #region Negative Tests

    [Fact]
    public void TagRegistry_RemoveTagsBehavior_UnregistersAllTags()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy").Add("goblin") }
        );

        // Act - Remove behavior
        BehaviorRegistry<TagsBehavior>.Instance.Remove(entity);

        // Assert - Tags should not resolve
        Assert.False(TagRegistry.Instance.TryResolve("enemy", out var enemyEntities) && enemyEntities.HasValue(entity.Index));
        Assert.False(TagRegistry.Instance.TryResolve("goblin", out var goblinEntities) && goblinEntities.HasValue(entity.Index));
    }

    [Fact]
    public void TagRegistry_UpdateTagsRemovingSome_OldTagsNoLongerResolve()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy").Add("goblin").Add("hostile") }
        );

        // Act - Update to remove "goblin" tag
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Without("goblin") }
        );

        // Assert
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var enemyEntities));
        Assert.True(enemyEntities.HasValue(entity.Index));

        Assert.True(TagRegistry.Instance.TryResolve("hostile", out var hostileEntities));
        Assert.True(hostileEntities.HasValue(entity.Index));

        // Goblin tag should no longer resolve to this entity
        if (TagRegistry.Instance.TryResolve("goblin", out var goblinEntities))
        {
            Assert.False(goblinEntities.HasValue(entity.Index));
        }
    }

    [Fact]
    public void TagRegistry_DeactivateEntityWithTags_UnregistersAllTags()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy").Add("goblin") }
        );

        // Act - Deactivate entity
        EntityRegistry.Instance.Deactivate(entity);

        // Assert - Tags should not resolve to this entity
        if (TagRegistry.Instance.TryResolve("enemy", out var enemyEntities))
        {
            Assert.False(enemyEntities.HasValue(entity.Index));
        }

        if (TagRegistry.Instance.TryResolve("goblin", out var goblinEntities))
        {
            Assert.False(goblinEntities.HasValue(entity.Index));
        }
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public void TagRegistry_BehaviorAddedEvent_RegistersTags()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();

        // Act - SetBehavior fires BehaviorAddedEvent
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );

        // Assert
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var entities));
        Assert.True(entities.HasValue(entity.Index));
    }

    [Fact]
    public void TagRegistry_BehaviorUpdatedEvent_UnregistersOldAndRegistersNew()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("old-tag") }
        );

        // Act - Update fires PreBehaviorUpdatedEvent then PostBehaviorUpdatedEvent
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Without("old-tag").Add("new-tag") }
        );

        // Assert
        Assert.True(TagRegistry.Instance.TryResolve("new-tag", out var newEntities));
        Assert.True(newEntities.HasValue(entity.Index));

        if (TagRegistry.Instance.TryResolve("old-tag", out var oldEntities))
        {
            Assert.False(oldEntities.HasValue(entity.Index));
        }
    }

    [Fact]
    public void TagRegistry_BehaviorRemovedEvent_UnregistersAllTags()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy").Add("boss") }
        );

        // Act - Remove fires PreBehaviorRemovedEvent
        BehaviorRegistry<TagsBehavior>.Instance.Remove(entity);

        // Assert
        if (TagRegistry.Instance.TryResolve("enemy", out var enemyEntities))
        {
            Assert.False(enemyEntities.HasValue(entity.Index));
        }

        if (TagRegistry.Instance.TryResolve("boss", out var bossEntities))
        {
            Assert.False(bossEntities.HasValue(entity.Index));
        }
    }

    [Fact]
    public void TagRegistry_EntityDeactivation_AutomaticallyUnregistersTags()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );

        // Act - Deactivate triggers PreBehaviorRemovedEvent cascade
        EntityRegistry.Instance.Deactivate(entity);

        // Assert
        if (TagRegistry.Instance.TryResolve("enemy", out var entities))
        {
            Assert.False(entities.HasValue(entity.Index));
        }
    }

    #endregion

    #region Partition Tests

    [Fact]
    public void TagRegistry_ResetEvent_ClearsSceneTagsOnly()
    {
        // Arrange
        var globalEntity = EntityRegistry.Instance.ActivateGlobal();
        var sceneEntity = EntityRegistry.Instance.Activate();
        globalEntity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );
        sceneEntity.SetBehavior<TagsBehavior>(
            (ref b) => b = b with { Tags = b.Tags.Add("enemy") }
        );

        // Act - ResetEvent clears scene entities
        EventBus<ResetEvent>.Push(new());

        // Assert - Global tag persists, scene tag cleared
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var entities));
        Assert.True(entities.HasValue(globalEntity.Index));
        Assert.False(entities.HasValue(sceneEntity.Index));
    }
    #endregion
}
