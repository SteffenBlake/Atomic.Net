using Atomic.Net.MonoGame;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Tags;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Tags.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class TagErrorHandlingTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    
    public TagErrorHandlingTests(ITestOutputHelper output)
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

    #region Error Event Validation

    [Fact]
    public void TagErrorHandling_NullTag_FiresErrorEventWithMessage()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - Error event fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e => 
            e.Message.Contains("null", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("value", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void TagErrorHandling_EmptyStringTag_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e =>
            e.Message.Contains("empty", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("whitespace", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void TagErrorHandling_WhitespaceOnlyTag_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e =>
            e.Message.Contains("whitespace", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("empty", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void TagErrorHandling_DuplicateTag_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e =>
            e.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void TagErrorHandling_InvalidCharacters_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.NotEmpty(errorListener.ReceivedEvents);
        Assert.Contains(errorListener.ReceivedEvents, e =>
            e.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("character", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void TagErrorHandling_AllErrors_DoNotThrowExceptions()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => SceneLoader.Instance.LoadGameScene(scenePath));
        Assert.Null(exception);
    }

    #endregion

    #region Error Recovery

    [Fact]
    public void TagErrorHandling_MixOfValidAndInvalidTags_ValidTagsRegistered()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - Valid tags from all entities should work
        Assert.True(EntityIdRegistry.Instance.TryResolve("null-tag", out var entity1));
        Assert.True(TagRegistry.Instance.TryResolve("valid", out var validEntities1));
        Assert.True(validEntities1.HasValue(entity1.Value.Index));

        Assert.True(EntityIdRegistry.Instance.TryResolve("empty-tag", out var entity2));
        Assert.True(TagRegistry.Instance.TryResolve("valid", out var validEntities2));
        Assert.True(validEntities2.HasValue(entity2.Value.Index));

        Assert.True(EntityIdRegistry.Instance.TryResolve("invalid-chars", out var entity3));
        Assert.True(TagRegistry.Instance.TryResolve("valid-tag", out var validTagEntities));
        Assert.True(validTagEntities.HasValue(entity3.Value.Index));
    }

    [Fact]
    public void TagErrorHandling_SceneWith10Entities3Invalid_7EntitiesLoadCorrectly()
    {
        // Arrange - Create a scene with mix of valid and invalid entities
        var scenePath = "/tmp/tags-mixed-validation.json";
        System.IO.File.WriteAllText(scenePath, @"{
  ""entities"": [
    { ""id"": ""entity1"", ""tags"": [""valid""] },
    { ""id"": ""entity2"", ""tags"": [""valid""] },
    { ""id"": ""entity3"", ""tags"": [""valid"", null] },
    { ""id"": ""entity4"", ""tags"": [""valid""] },
    { ""id"": ""entity5"", ""tags"": [""valid"", """"] },
    { ""id"": ""entity6"", ""tags"": [""valid""] },
    { ""id"": ""entity7"", ""tags"": [""valid""] },
    { ""id"": ""entity8"", ""tags"": [""valid"", ""  ""] },
    { ""id"": ""entity9"", ""tags"": [""valid""] },
    { ""id"": ""entity10"", ""tags"": [""valid""] }
  ]
}");

        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - All 10 entities should load (errors don't stop loading)
        for (var i = 1; i <= 10; i++)
        {
            Assert.True(EntityIdRegistry.Instance.TryResolve($"entity{i}", out var entity));
            Assert.True(TagRegistry.Instance.TryResolve("valid", out var validEntities));
            Assert.True(validEntities.HasValue(entity.Value.Index));
        }

        // 3 errors should be fired (for entities 3, 5, 8)
        Assert.True(errorListener.ReceivedEvents.Count >= 3);
    }

    [Fact]
    public void TagErrorHandling_LoadSceneWithErrors_ThenLoadCleanScene_NewSceneLoadsClean()
    {
        // Arrange
        var invalidScenePath = "Tags/Fixtures/tags-invalid.json";
        var validScenePath = "Tags/Fixtures/tags-basic.json";

        // Act - Load scene with errors
        var errorListener1 = new FakeEventListener<ErrorEvent>();
        SceneLoader.Instance.LoadGameScene(invalidScenePath);
        var errorCount1 = errorListener1.ReceivedEvents.Count;
        
        // Reset and load clean scene
        EventBus<ResetEvent>.Push(new());
        var errorListener2 = new FakeEventListener<ErrorEvent>();
        SceneLoader.Instance.LoadGameScene(validScenePath);
        var errorCount2 = errorListener2.ReceivedEvents.Count;

        // Assert - First scene had errors, second scene had none
        Assert.True(errorCount1 > 0);
        Assert.Equal(0, errorCount2);

        // Clean scene entities should work correctly
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1));
        Assert.True(TagRegistry.Instance.TryResolve("enemy", out var enemyEntities));
        Assert.True(enemyEntities.HasValue(goblin1.Value.Index));
    }

    #endregion

    #region Graceful Degradation

    [Fact]
    public void TagErrorHandling_SceneLoadingContinues_DespiteInvalidTags()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - All entities should be loaded despite tag errors
        Assert.True(EntityIdRegistry.Instance.TryResolve("null-tag", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("empty-tag", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("whitespace-tag", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("duplicate-tags", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("invalid-chars", out _));
    }

    #endregion
}
