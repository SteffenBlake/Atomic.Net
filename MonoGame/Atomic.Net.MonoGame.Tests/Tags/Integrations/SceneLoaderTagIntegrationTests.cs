using Atomic.Net.MonoGame;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Tags;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Tags.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class SceneLoaderTagIntegrationTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    
    public SceneLoaderTagIntegrationTests(ITestOutputHelper output)
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

    #region Scene Loading

    [Fact]
    public void SceneLoader_LoadBasicTagScene_EntitiesHaveTagsRegistered()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-basic.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-2", out var goblin2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("boss", out var boss));

        // Check tag registry
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var enemyEntities));
        Assert.True(enemyEntities.HasValue(goblin1.Value.Index));
        Assert.True(enemyEntities.HasValue(goblin2.Value.Index));
        Assert.True(enemyEntities.HasValue(boss.Value.Index));

        Assert.True(TagRegistry.Instance.TryResolve("goblin", out var goblinEntities));
        Assert.True(goblinEntities.HasValue(goblin1.Value.Index));
        Assert.True(goblinEntities.HasValue(goblin2.Value.Index));
        Assert.False(goblinEntities.HasValue(boss.Value.Index));

        Assert.True(TagRegistry.Instance.TryResolve("boss", out var bossEntities));
        Assert.True(bossEntities.HasValue(boss.Value.Index));
        Assert.False(bossEntities.HasValue(goblin1.Value.Index));
    }

    [Fact]
    public void SceneLoader_EntityWithEmptyTagsArray_HasBehaviorButNoTags()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-edge-cases.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("empty-tags", out var entity));
        
        // Entity has TagsBehavior
        Assert.True(entity.Value.TryGetBehavior<TagsBehavior>(out var behavior));
        
        // But tags are empty
        Assert.Empty(behavior.Value.Tags);
    }

    [Fact]
    public void SceneLoader_EntityMissingTagsField_DoesNotHaveTagsBehavior()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-edge-cases.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("no-tags-field", out var entity));
        Assert.False(entity.Value.TryGetBehavior<TagsBehavior>(out _));
    }

    [Fact]
    public void SceneLoader_TagsWithMixedCase_NormalizedToLowercase()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-edge-cases.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("case-test", out var entity));

        // All case variations should resolve to same entity
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var enemyEntities));
        Assert.True(enemyEntities.HasValue(entity.Value.Index));

        Assert.True(TagRegistry.Instance.TryResolve("boss", out var bossEntities));
        Assert.True(bossEntities.HasValue(entity.Value.Index));

        Assert.True(TagRegistry.Instance.TryResolve("flying", out var flyingEntities));
        Assert.True(flyingEntities.HasValue(entity.Value.Index));
    }

    [Fact]
    public void SceneLoader_ManyTags_AllRegisteredCorrectly()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-many.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-with-many-tags", out var entity));

        // Verify all 50 tags are registered
        for (var i = 1; i <= 50; i++)
        {
            var tagName = $"tag{i}";
            Assert.True(TagRegistry.Instance.TryResolve(tagName, out var entities));
            Assert.True(entities.HasValue(entity.Value.Index));
        }
    }

    #endregion

    #region Scene Loading Errors

    [Fact]
    public void SceneLoader_NullTagInArray_FiresErrorAndSkipsTag()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("null-tag", out var entity));

        // Valid tags should be registered
        Assert.True(TagRegistry.Instance.TryResolve("valid", out var validEntities));
        Assert.True(validEntities.HasValue(entity.Value.Index));

        Assert.True(TagRegistry.Instance.TryResolve("also-valid", out var alsoValidEntities));
        Assert.True(alsoValidEntities.HasValue(entity.Value.Index));

        // Error should be fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void SceneLoader_EmptyStringTag_FiresErrorAndSkipsTag()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("empty-tag", out var entity));

        // Valid tags should be registered
        Assert.True(TagRegistry.Instance.TryResolve("valid", out var validEntities));
        Assert.True(validEntities.HasValue(entity.Value.Index));

        Assert.True(TagRegistry.Instance.TryResolve("also-valid", out var alsoValidEntities));
        Assert.True(alsoValidEntities.HasValue(entity.Value.Index));

        // Error should be fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void SceneLoader_WhitespaceTag_FiresErrorAndSkipsTag()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("whitespace-tag", out var entity));

        // Valid tags should be registered
        Assert.True(TagRegistry.Instance.TryResolve("valid", out var validEntities));
        Assert.True(validEntities.HasValue(entity.Value.Index));

        Assert.True(TagRegistry.Instance.TryResolve("also-valid", out var alsoValidEntities));
        Assert.True(alsoValidEntities.HasValue(entity.Value.Index));

        // Error should be fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void SceneLoader_DuplicateTags_FiresErrorAndKeepsFirst()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("duplicate-tags", out var entity));

        // Tags should be registered (only once)
        Assert.True(entity.Value.TryGetBehavior<TagsBehavior>(out var behavior));
        Assert.Equal(2, behavior.Value.Tags.Count); // "enemy" and "boss"

        // Error should be fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void SceneLoader_InvalidCharactersInTag_FiresErrorAndSkipsTag()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("invalid-chars", out var entity));

        // Valid tags should be registered
        Assert.True(TagRegistry.Instance.TryResolve("valid-tag", out var validEntities));
        Assert.True(validEntities.HasValue(entity.Value.Index));

        Assert.True(TagRegistry.Instance.TryResolve("also_valid", out var alsoValidEntities));
        Assert.True(alsoValidEntities.HasValue(entity.Value.Index));

        // "invalid tag" with space should NOT be registered
        var hasInvalidTag = TagRegistry.Instance.TryResolve("invalid tag", out var invalidEntities);
        Assert.False(hasInvalidTag && invalidEntities.HasValue(entity.Value.Index));

        // Error should be fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    #endregion

    #region Partition Behavior

    [Fact]
    public void SceneLoader_LoadGlobalSceneWithTags_TagsPersistThroughReset()
    {
        // Arrange - Create a temporary global scene file
        var globalScenePath = "/tmp/tags-global-test.json";
        System.IO.File.WriteAllText(globalScenePath, @"{
  ""entities"": [
    {
      ""id"": ""global-entity"",
      ""tags"": [""global-tag""]
    }
  ]
}");

        // Act
        SceneLoader.Instance.LoadGlobalScene(globalScenePath);

        Assert.True(EntityIdRegistry.Instance.TryResolve("global-entity", out var globalEntity));
        Assert.True(TagRegistry.Instance.TryResolve("global-tag", out var globalTags));
        Assert.True(globalTags.HasValue(globalEntity.Value.Index));

        // Reset
        EventBus<ResetEvent>.Push(new());

        // Assert - Global tags persist
        Assert.True(TagRegistry.Instance.TryResolve("global-tag", out var globalTagsAfterReset));
        Assert.True(globalTagsAfterReset.HasValue(globalEntity.Value.Index));
    }

    [Fact]
    public void SceneLoader_LoadGameSceneWithTags_TagsClearedOnReset()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-basic.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1));
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var enemyTags));
        Assert.True(enemyTags.HasValue(goblin1.Value.Index));

        // Reset
        EventBus<ResetEvent>.Push(new());

        // Assert - Scene tags cleared
        var hasEnemyTag = TagRegistry.Instance.TryResolve("enemy", out var enemyTagsAfterReset);
        Assert.False(hasEnemyTag && enemyTagsAfterReset.HasValue(goblin1.Value.Index));
    }

    [Fact]
    public void SceneLoader_LoadBothGlobalAndSceneTags_OnlySceneTagsClearedOnReset()
    {
        // Arrange - Create global scene
        var globalScenePath = "/tmp/tags-global-test2.json";
        System.IO.File.WriteAllText(globalScenePath, @"{
  ""entities"": [
    {
      ""id"": ""global-enemy"",
      ""tags"": [""enemy""]
    }
  ]
}");

        SceneLoader.Instance.LoadGlobalScene(globalScenePath);
        SceneLoader.Instance.LoadGameScene("Tags/Fixtures/tags-basic.json");

        Assert.True(EntityIdRegistry.Instance.TryResolve("global-enemy", out var globalEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var sceneEntity));

        // Act - Reset
        EventBus<ResetEvent>.Push(new());

        // Assert - Global entity still has tag, scene entity doesn't
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var enemyTags));
        Assert.True(enemyTags.HasValue(globalEntity.Value.Index));
        Assert.False(enemyTags.HasValue(sceneEntity.Value.Index));
    }

    #endregion
}
