using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Sequencing;

namespace Atomic.Net.MonoGame.Tests.Sequencing.Integrations;

/// <summary>
/// Integration tests for SequenceDriver execution.
/// Tests delay, do, tween, and repeat steps with full pipeline.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class SequenceDriverIntegrationTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;

    public SequenceDriverIntegrationTests()
    {
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        _errorLogger = new ErrorEventLogger();
    }

    public void Dispose()
    {
        // Clean up between tests
        _errorLogger.Dispose();
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void DelayStep_WaitsForDuration_ThenAdvances()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/delay-sequence.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Check for errors
        Assert.Empty(_errorLogger.Errors);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Start sequence via rule
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Act - Run frames during delay
        SequenceDriver.Instance.RunFrame(0.3f); // 0.3s elapsed, delay is 0.5s
        
        // Assert - Property should NOT be set yet (still in delay)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props1));
        Assert.False(props1.Value.Properties.ContainsKey("delayComplete"));
        
        // Act - Complete delay
        SequenceDriver.Instance.RunFrame(0.3f); // 0.6s total elapsed, delay complete
        
        // Need one more frame to execute the do step after delay completes
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert - Property should now be set
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("delayComplete", out var value));
        value.Visit(
            s => Assert.Equal("true", s),
            f => Assert.Fail("Expected string"),
            b => Assert.Fail("Expected string"),
            () => Assert.Fail("Expected value")
        );
    }
}
