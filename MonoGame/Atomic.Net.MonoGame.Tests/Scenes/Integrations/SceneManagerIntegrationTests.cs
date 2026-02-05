using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Tests;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

/// <summary>
/// Integration tests for async scene loading with SceneManager.
/// Tests the full async pipeline: LoadScene → Progress Tracking → GC → Completion
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class SceneManagerIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ErrorEventLogger _errorLogger;

    public SceneManagerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _errorLogger = new ErrorEventLogger(output);

        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        _errorLogger.Dispose();
        // Clean up ALL entities (both global and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public async Task LoadScene_WithValidPath_LoadsSceneInBackground()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/scene1.json";

        // Act
        SceneManager.Instance.LoadScene(scenePath);

        // Assert - IsLoading should be true initially
        Assert.True(SceneManager.Instance.IsLoading, "IsLoading should be true immediately after LoadScene");
        Assert.True(SceneManager.Instance.LoadingProgress < 1.0f, "LoadingProgress should be < 1.0 during load");

        // Wait for loading to complete (poll Update() and check IsLoading)
        var maxWait = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        while (SceneManager.Instance.IsLoading && DateTime.UtcNow - start < maxWait)
        {
            SceneManager.Instance.Update();
            await Task.Delay(10);
        }

        // Assert - Loading should complete
        Assert.False(SceneManager.Instance.IsLoading, "IsLoading should be false after load completes");
        Assert.Equal(1.0f, SceneManager.Instance.LoadingProgress);

        // Verify entity was loaded
        var activeEntities = EntityRegistry.Instance.GetActiveSceneEntities().ToList();
        Assert.Single(activeEntities);

        var entity = activeEntities[0];
        Assert.True(entity.TryGetBehavior<PropertiesBehavior>(out var props));
        Assert.Equal("scene1", props.Value.Properties["sceneName"]);
    }

    [Fact]
    public async Task LoadScene_WithFileNotFound_ReturnsToIdleState()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/nonexistent.json";

        // Act
        SceneManager.Instance.LoadScene(scenePath);

        // Wait a bit for background task to process error
        await Task.Delay(100);
        SceneManager.Instance.Update(); // Process error queue

        // Assert - Should return to idle state
        Assert.False(SceneManager.Instance.IsLoading, "IsLoading should be false after error");
        Assert.Equal(1.0f, SceneManager.Instance.LoadingProgress);
    }

    [Fact]
    public async Task LoadScene_WithCorruptJSON_ReturnsToIdleState()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/corrupt-scene.json";

        // Act
        SceneManager.Instance.LoadScene(scenePath);

        // Wait a bit for background task to process error
        await Task.Delay(100);
        SceneManager.Instance.Update(); // Process error queue

        // Assert - Should return to idle state
        Assert.False(SceneManager.Instance.IsLoading, "IsLoading should be false after error");
        Assert.Equal(1.0f, SceneManager.Instance.LoadingProgress);
    }

    [Fact]
    public async Task LoadScene_WithInvalidTags_ReturnsToIdleState()
    {
        // Arrange - Scene has invalid tags which will cause deserialization to fail
        var scenePath = "Scenes/Fixtures/invalid-tags-scene.json";
        _errorLogger.Clear(); // Clear any previous errors

        // Act
        SceneManager.Instance.LoadScene(scenePath);

        // Wait a bit for background task to process error
        await Task.Delay(100);
        SceneManager.Instance.Update(); // Process error queue

        // Assert - Should return to idle state with error
        Assert.False(SceneManager.Instance.IsLoading, "IsLoading should be false after error");
        Assert.Equal(1.0f, SceneManager.Instance.LoadingProgress);
        
        // Assert - Should have logged error about invalid tags from JsonException
        Assert.Single(_errorLogger.Errors);
        Assert.Contains("Tag array contains empty or whitespace-only tag", _errorLogger.Errors[0]);
    }

    [Fact]
    public async Task LoadScene_WithInvalidRules_ReturnsToIdleState()
    {
        // Arrange - This replicates the EXACT issue that was causing the original test failures
        // The scene has rules with "mut" command that can't be deserialized
        var scenePath = "Scenes/Fixtures/invalid-rules-scene.json";
        _errorLogger.Clear(); // Clear any previous errors

        // Act
        SceneManager.Instance.LoadScene(scenePath);

        // Wait a bit for background task to process error
        await Task.Delay(100);
        SceneManager.Instance.Update(); // Process error queue

        // Assert - Should return to idle state with error
        Assert.False(SceneManager.Instance.IsLoading, "IsLoading should be false after error");
        Assert.Equal(1.0f, SceneManager.Instance.LoadingProgress);
        
        // Assert - Should have logged exactly 1 error about JSON deserialization failure
        Assert.Single(_errorLogger.Errors);
        Assert.Contains("Failed to parse scene JSON", _errorLogger.Errors[0]);
        Assert.Contains("could not be converted", _errorLogger.Errors[0]);
    }

    [Fact]
    public async Task LoadScene_ClearsScenePartition_BeforeLoad()
    {
        // Arrange - Load scene1 first
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/scene1.json");
        var scene1Entities = EntityRegistry.Instance.GetActiveSceneEntities().ToList();
        Assert.Single(scene1Entities);

        // Act - Load scene2 asynchronously
        SceneManager.Instance.LoadScene("Scenes/Fixtures/scene2.json");

        // Wait for loading to complete
        var maxWait = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        while (SceneManager.Instance.IsLoading && DateTime.UtcNow - start < maxWait)
        {
            SceneManager.Instance.Update();
            await Task.Delay(10);
        }

        // Assert - Only scene2 entities should exist
        var scene2Entities = EntityRegistry.Instance.GetActiveSceneEntities().ToList();
        Assert.Single(scene2Entities);

        var entity = scene2Entities[0];
        Assert.True(entity.TryGetBehavior<PropertiesBehavior>(out var props));
        Assert.Equal("scene2", props.Value.Properties["sceneName"]);
    }
}
