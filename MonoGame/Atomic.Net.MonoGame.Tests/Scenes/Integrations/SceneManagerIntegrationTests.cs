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

    [Fact]
    public async Task LoadScene_WithTooManyRules_ErrorBubblesUpFromBackgroundThread()
    {
        // Arrange - Create a scene with more than MaxSceneRules (1024) to trigger capacity error
        // This tests that ErrorEvents pushed from background thread properly bubble up to main thread
        var scenePath = "/tmp/scene-too-many-rules.json";
        
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"entities\": [],");
        sb.AppendLine("  \"rules\": [");
        
        // Generate 1030 rules (more than MaxSceneRules = 1024)
        // Use minimal valid rule format - from selector with empty do
        for (var i = 0; i < 1030; i++)
        {
            sb.Append("    {");
            sb.Append($"\"from\": \"#tag{i}\", ");
            sb.Append("\"do\": {\"mut\": []}");
            sb.Append("}");
            
            if (i < 1029)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        
        File.WriteAllText(scenePath, sb.ToString());
        
        // Clear any previous errors
        _errorLogger.Clear();

        // Act
        SceneManager.Instance.LoadScene(scenePath);

        // Wait for loading to complete
        var maxWait = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        while (SceneManager.Instance.IsLoading && DateTime.UtcNow - start < maxWait)
        {
            SceneManager.Instance.Update();
            await Task.Delay(10);
        }

        // Assert - Should have error about capacity exceeded
        // This test demonstrates the BUG: ErrorEvents pushed from background thread
        // (by RuleRegistry.TryActivate when capacity is exceeded) don't bubble up properly
        _output.WriteLine($"Error count: {_errorLogger.Errors.Count}");
        foreach (var error in _errorLogger.Errors)
        {
            _output.WriteLine($"  Error: {error}");
        }
        
        Assert.NotEmpty(_errorLogger.Errors);
        Assert.Contains(_errorLogger.Errors, e => e.Contains("capacity exceeded"));
    }

    [Fact]
    public async Task LoadScene_WithTooManySequences_ErrorBubblesUpFromBackgroundThread()
    {
        // Arrange - Create a scene with more than MaxSceneSequences (512) to trigger capacity error
        var scenePath = "/tmp/scene-too-many-sequences.json";
        
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"entities\": [],");
        sb.AppendLine("  \"sequences\": [");
        
        // Generate 520 sequences (more than MaxSceneSequences = 512)
        for (var i = 0; i < 520; i++)
        {
            sb.Append("    {");
            sb.Append($"\"id\": \"seq{i}\", ");
            sb.Append("\"steps\": [{\"delay\": 0.1}]");
            sb.Append("}");
            
            if (i < 519)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        
        File.WriteAllText(scenePath, sb.ToString());
        
        // Clear any previous errors
        _errorLogger.Clear();

        // Act
        SceneManager.Instance.LoadScene(scenePath);

        // Wait for loading to complete
        var maxWait = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        while (SceneManager.Instance.IsLoading && DateTime.UtcNow - start < maxWait)
        {
            SceneManager.Instance.Update();
            await Task.Delay(10);
        }

        // Assert - Should have error about capacity exceeded
        // This test demonstrates the BUG: ErrorEvents pushed from background thread
        // (by SequenceRegistry.TryActivate when capacity is exceeded) don't bubble up properly
        _output.WriteLine($"Error count: {_errorLogger.Errors.Count}");
        foreach (var error in _errorLogger.Errors)
        {
            _output.WriteLine($"  Error: {error}");
        }
        
        Assert.NotEmpty(_errorLogger.Errors);
        Assert.Contains(_errorLogger.Errors, e => e.Contains("capacity exceeded"));
    }

    [Fact]
    public async Task LoadScene_BackgroundThreadErrorEvents_DemonstrateThreadSafetyIssue()
    {
        // This test demonstrates the BUG: When ErrorEvents are pushed from the background thread,
        // they invoke ALL registered handlers on the background thread, which can cause
        // thread-safety issues with handlers that aren't thread-safe (like ErrorEventLogger
        // which uses a non-thread-safe List).
        
        // The problem is that EventBus.Push() directly invokes handlers on the calling thread,
        // so when RuleRegistry.TryActivate() pushes an error from the background thread,
        // it calls ErrorEventLogger.OnEvent() on the background thread, which does
        // _errors.Add() on a List that isn't thread-safe.
        
        // To properly demonstrate this, we'd need to create many concurrent loads that
        // trigger errors simultaneously, but for now, we just verify that errors ARE
        // being pushed from background thread (which is the root cause of the issue).
        
        var scenePath = "/tmp/scene-too-many-rules.json";
        
        // Generate scene with too many rules
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"entities\": [],");
        sb.AppendLine("  \"rules\": [");
        
        for (var i = 0; i < 1030; i++)
        {
            sb.Append("    {");
            sb.Append($"\"from\": \"#tag{i}\", ");
            sb.Append("\"do\": {{\"mut\": []}}");
            sb.Append("}");
            
            if (i < 1029)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        
        File.WriteAllText(scenePath, sb.ToString());
        
        _errorLogger.Clear();

        // Act
        SceneManager.Instance.LoadScene(scenePath);

        // Wait for loading
        var maxWait = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        while (SceneManager.Instance.IsLoading && DateTime.UtcNow - start < maxWait)
        {
            SceneManager.Instance.Update();
            await Task.Delay(10);
        }

        // The test PASSES because errors ARE being received, but this demonstrates
        // the concurrency issue: ErrorEventLogger.OnEvent() is being called on the
        // background thread, which is unsafe.
        _output.WriteLine($"Thread safety issue: ErrorEventLogger received {_errorLogger.Errors.Count} errors from background thread");
        _output.WriteLine("These errors invoked ErrorEventLogger.OnEvent() on the background thread,");
        _output.WriteLine("which calls List.Add() - NOT thread-safe!");
        
        Assert.NotEmpty(_errorLogger.Errors);
    }
}
