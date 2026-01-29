using Xunit;
using Xunit.Abstractions;
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
    private readonly ITestOutputHelper _output;

    public SequenceDriverIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        _errorLogger = new ErrorEventLogger(output);
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
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Start sequence via rule
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Act - Run frames during delay
        SequenceDriver.Instance.RunFrame(0.3f);
        
        // Assert - Property should NOT be set yet (still in delay)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props1));
        Assert.False(props1.Value.Properties.ContainsKey("delayComplete"));
        
        // Act - Complete delay
        SequenceDriver.Instance.RunFrame(0.3f);
        
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

    [Fact]
    public void DoStep_ExecutesCommandImmediately()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/do-sequence.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Act - Start sequence via rule
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Run one frame to execute the do step
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert - Property should be set immediately
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("stepExecuted", out var value));
        value.Visit(
            s => Assert.Equal("true", s),
            f => Assert.Fail("Expected string"),
            b => Assert.Fail("Expected string"),
            () => Assert.Fail("Expected value")
        );
    }

    [Fact]
    public void TweenStep_InterpolatesValue_OverDuration()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/tween-sequence.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Start sequence via rule
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Act - Run at 50% of tween duration
        SequenceDriver.Instance.RunFrame(0.5f);
        
        // Assert - Opacity should be around 0.5
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props1));
        Assert.True(props1.Value.Properties.TryGetValue("opacity", out var value1));
        value1.Visit(
            s => Assert.Fail("Expected float"),
            f => {
                Assert.True(f >= 0.45f);
                Assert.True(f <= 0.55f);
            },
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
        
        // Act - Complete tween
        SequenceDriver.Instance.RunFrame(0.6f);
        
        // Assert - Opacity should be 1.0
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("opacity", out var value2));
        value2.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(1.0f, f, precision: 2),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    [Fact]
    public void RepeatStep_ExecutesAtIntervals_UntilCondition()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/repeat-sequence.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Start sequence via rule
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Act - Run for 0.6 seconds (should trigger at 0.5s = 1 execution)
        SequenceDriver.Instance.RunFrame(0.6f);
        
        // Assert - Counter should be 1
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props1));
        Assert.True(props1.Value.Properties.TryGetValue("counter", out var counter1));
        counter1.Visit(
            s => Assert.Fail("Expected float"),
            f => {
                _output.WriteLine($"After 0.6s: counter={f}");
                Assert.Equal(1f, f);
            },
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
        
        // Act - Run another 0.9 seconds (total 1.5s, should trigger at 1.0s and 1.5s = 2 more executions)
        SequenceDriver.Instance.RunFrame(0.9f);
        
        // Assert - Counter should be 3
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("counter", out var counter2));
        counter2.Visit(
            s => Assert.Fail("Expected float"),
            f => {
                _output.WriteLine($"After 1.5s total: counter={f}");
                Assert.Equal(3f, f);
            },
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
        
        // Act - Run until condition met (total >= 2.0s)
        SequenceDriver.Instance.RunFrame(0.6f);
        
        // Assert - Repeat complete marker should be set
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props3));
        Assert.True(props3.Value.Properties.ContainsKey("repeatComplete"));
    }

    [Fact]
    public void SequenceStartCommand_StartsSequence()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/sequence-commands.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Act - Manually start sequence using command driver
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var sequenceIndex));
        SequenceStartCmdDriver.Execute(player.Value.Index, "test-sequence");
        
        // Run past delay
        SequenceDriver.Instance.RunFrame(0.6f);
        
        // Assert - Value should be set
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("value", out var value));
        value.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(42f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    [Fact]
    public void SequenceStopCommand_StopsRunningSequence()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/sequence-commands.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Start sequence
        SequenceStartCmdDriver.Execute(player.Value.Index, "test-sequence");
        
        // Run for 0.3s (still in delay)
        SequenceDriver.Instance.RunFrame(0.3f);
        
        // Act - Stop sequence
        SequenceStopCmdDriver.Execute(player.Value.Index, "test-sequence");
        
        // Run past original delay completion
        SequenceDriver.Instance.RunFrame(0.4f);
        
        // Assert - Value should NOT be set (sequence was stopped)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props));
        Assert.False(props.Value.Properties.ContainsKey("value"));
    }

    [Fact]
    public void SequenceResetCommand_RestartsSequence()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/sequence-commands.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Start sequence
        SequenceStartCmdDriver.Execute(player.Value.Index, "test-sequence");
        
        // Run for 0.3s (partial delay)
        SequenceDriver.Instance.RunFrame(0.3f);
        
        // Act - Reset sequence
        SequenceResetCmdDriver.Execute(player.Value.Index, "test-sequence");
        
        // Run for 0.3s again (should NOT complete, reset to beginning)
        SequenceDriver.Instance.RunFrame(0.3f);
        
        // Assert - Value should NOT be set yet
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props1));
        Assert.False(props1.Value.Properties.ContainsKey("value"));
        
        // Run another 0.3s to complete delay
        SequenceDriver.Instance.RunFrame(0.3f);
        
        // Assert - Value should now be set
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props2));
        Assert.True(props2.Value.Properties.ContainsKey("value"));
    }

    [Fact]
    public void EntityDeactivation_CancelsAllSequences()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/sequence-commands.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Start sequence
        SequenceStartCmdDriver.Execute(player.Value.Index, "test-sequence");
        
        // Run for 0.3s (still in delay)
        SequenceDriver.Instance.RunFrame(0.3f);
        
        // Act - Deactivate entity
        EntityRegistry.Instance.Deactivate(player.Value);
        
        // Run past original delay completion
        SequenceDriver.Instance.RunFrame(0.4f);
        
        // Assert - No errors should have occurred
        Assert.Empty(_errorLogger.Errors);
    }

    [Fact]
    public void SequenceCompletion_AutoRemovesFromRegistry()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/do-sequence.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Act - Start and complete sequence
        RulesDriver.Instance.RunFrame(0.016f);
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert - Property was set (sequence executed)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props));
        Assert.True(props.Value.Properties.ContainsKey("stepExecuted"));
        
        // Run another frame - sequence should be gone, no errors
        SequenceDriver.Instance.RunFrame(0.016f);
        Assert.Empty(_errorLogger.Errors);
    }

    [Fact]
    public void InvalidSequenceId_EmitsErrorEvent()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/invalid-sequence.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Act - Try to start non-existent sequence
        SequenceStartCmdDriver.Execute(player.Value.Index, "non-existent-sequence");
        
        // Assert - Error event should be emitted
        Assert.NotEmpty(_errorLogger.Errors);
        Assert.Contains(_errorLogger.Errors, e => e.Contains("non-existent-sequence"));
    }

    [Fact]
    public void ConcurrentSequences_OnSameEntity_BothExecute()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/concurrent-sequences.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Act - Start both sequences
        SequenceStartCmdDriver.Execute(player.Value.Index, "sequence1");
        SequenceStartCmdDriver.Execute(player.Value.Index, "sequence2");
        
        // Run one frame
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert - Both properties should be set
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("counter1", out var value1));
        Assert.True(props.Value.Properties.TryGetValue("counter2", out var value2));
        
        value1.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(1f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
        
        value2.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(2f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    [Fact]
    public void SequenceRestart_AfterCompletion_Works()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/do-sequence.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Start and complete sequence first time
        RulesDriver.Instance.RunFrame(0.016f);
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Reset property so rule can trigger again
        player.Value.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.Without("stepExecuted") };
        });
        
        // Act - Start sequence again
        RulesDriver.Instance.RunFrame(0.016f);
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert - Property should be set again
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props));
        Assert.True(props.Value.Properties.ContainsKey("stepExecuted"));
    }

    [Fact]
    public void SequenceChaining_StartsSecondSequence()
    {
        // Arrange
        var scenePath = "Sequencing/Fixtures/chaining-sequence.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.Empty(_errorLogger.Errors);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        
        // Act - Start first sequence
        SequenceStartCmdDriver.Execute(player.Value.Index, "first-sequence");
        
        // Run one frame to execute both steps of first sequence
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Run another frame to execute second sequence
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert - Both properties should be set
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(player.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("step1", out var value1));
        Assert.True(props.Value.Properties.TryGetValue("step2", out var value2));
        
        value1.Visit(
            s => Assert.Fail("Expected bool"),
            f => Assert.Fail("Expected bool"),
            b => Assert.True(b),
            () => Assert.Fail("Expected value")
        );
        
        value2.Visit(
            s => Assert.Fail("Expected bool"),
            f => Assert.Fail("Expected bool"),
            b => Assert.True(b),
            () => Assert.Fail("Expected value")
        );
    }
}
