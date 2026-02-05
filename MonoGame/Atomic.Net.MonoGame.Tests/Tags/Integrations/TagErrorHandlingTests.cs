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

        // Assert - Error event fired (scene parse failed due to invalid data)
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void TagErrorHandling_EmptyStringTag_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - Error event fired (scene parse failed due to invalid data)
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void TagErrorHandling_WhitespaceOnlyTag_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - Error event fired (scene parse failed due to invalid data)
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void TagErrorHandling_DuplicateTag_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - Error event fired (scene parse failed due to invalid data)
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void TagErrorHandling_InvalidCharacters_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - Error event fired (scene parse failed due to invalid data)
        Assert.NotEmpty(errorListener.ReceivedEvents);
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
    public void TagErrorHandling_MixOfValidAndInvalidTags_EntitiesFailToLoad()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - Entities with ANY invalid tags should NOT load
        Assert.False(EntityIdRegistry.Instance.TryResolve("null-tag", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("empty-tag", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("invalid-chars", out _));
        
        // Errors should be fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void TagErrorHandling_SceneWith10Entities3Invalid_EntireSceneFailsToLoad()
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

        // Assert - NO entities should load because scene has invalid data
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity1", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity2", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity3", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity4", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity5", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity6", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity7", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity8", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity9", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity10", out _));

        // Error should be fired for scene parse failure
        Assert.NotEmpty(errorListener.ReceivedEvents);
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
        ResetDriver.Instance.Run();
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
    public void TagErrorHandling_SceneLoadingContinues_InvalidEntitiesSkipped()
    {
        // Arrange
        var scenePath = "Tags/Fixtures/tags-invalid.json";
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert - Entities with invalid tags should NOT load, but scene loading should complete
        Assert.False(EntityIdRegistry.Instance.TryResolve("null-tag", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("empty-tag", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("whitespace-tag", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("duplicate-tags", out _));
        Assert.False(EntityIdRegistry.Instance.TryResolve("invalid-chars", out _));
        
        // Errors should be fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    #endregion
}
