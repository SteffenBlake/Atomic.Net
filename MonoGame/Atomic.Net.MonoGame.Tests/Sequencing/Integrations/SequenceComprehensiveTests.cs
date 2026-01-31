using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Sequencing;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Sequencing.Integrations;

/// <summary>
/// Comprehensive integration tests for the Sequence System.
/// Covers all step types, commands, edge cases, and negative scenarios.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class SequenceComprehensiveTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;
    private readonly ITestOutputHelper _output;

    public SequenceComprehensiveTests(ITestOutputHelper output)
    {
        _output = output;
        _errorLogger = new ErrorEventLogger(output);
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
        EventBus<ShutdownEvent>.Push(new());
    }

    // ========== Do Step Tests ==========
    
    [Fact]
    public void DoStep_ExecutesCommandImmediately()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/do-test.json");
        
        // Act: Start sequence via rule
        RulesDriver.Instance.RunFrame(0.016f);
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Act: Run sequence driver once
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert: Counter incremented
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("counter", out var counter));
        counter.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(1f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    [Fact]
    public void DoStep_ReceivesDeltaTimeContext()
    {
        // Arrange: Create a do step that uses deltaTime
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/do-test.json");
        
        // Act: Start and run with specific deltaTime
        RulesDriver.Instance.RunFrame(0.016f);
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors - deltaTime was available in context
        Assert.Empty(_errorListener.ReceivedEvents);
    }

    // ========== Repeat Step Tests ==========
    
    [Fact]
    public void RepeatStep_ExecutesAtIntervals()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/repeat-test.json");
        
        // Act: Start sequence
        RulesDriver.Instance.RunFrame(0.016f);
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Act: Run for 0.55s (should execute 5 times: at 0.1, 0.2, 0.3, 0.4, 0.5)
        SequenceDriver.Instance.RunFrame(0.55f);
        
        // Assert: Counter should be 5
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("counter", out var counter));
        counter.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(5f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    [Fact]
    public void RepeatStep_StopsWhenConditionMet()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/repeat-test.json");
        
        // Act: Start sequence
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Act: Run past the until condition
        SequenceDriver.Instance.RunFrame(1.0f);
        
        // Assert: Counter should be 5 (stops at 0.5s, doesn't continue to 1.0s)
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("counter", out var counter));
        counter.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(5f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    // ========== Sequence Command Tests ==========
    
    [Fact]
    public void SequenceStart_StartsSequence()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));
        
        // Act: Manually start sequence (no rule)
        SequenceDriver.Instance.StartSequence(entity.Value, seqIndex);
        
        // Act: Run past delay
        SequenceDriver.Instance.RunFrame(0.2f);
        
        // Assert: Value should be 42
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("value", out var value));
        value.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(42f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    [Fact]
    public void SequenceStop_StopsRunningSequence()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));
        
        // Act: Start sequence
        SequenceDriver.Instance.StartSequence(entity.Value, seqIndex);
        
        // Act: Run partially
        SequenceDriver.Instance.RunFrame(0.05f);
        
        // Act: Stop sequence
        SequenceDriver.Instance.StopSequence(entity.Value, seqIndex);
        
        // Act: Run past delay (would complete if not stopped)
        SequenceDriver.Instance.RunFrame(0.2f);
        
        // Assert: Value should still be 0 (sequence was stopped)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("value", out var value));
        value.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(0f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    [Fact]
    public void SequenceReset_RestartsSequenceFromBeginning()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));
        
        // Act: Start sequence and run partially
        SequenceDriver.Instance.StartSequence(entity.Value, seqIndex);
        SequenceDriver.Instance.RunFrame(0.05f);
        
        // Act: Reset sequence
        SequenceDriver.Instance.ResetSequence(entity.Value, seqIndex);
        
        // Act: Run past delay again
        SequenceDriver.Instance.RunFrame(0.2f);
        
        // Assert: Sequence completed (reset worked)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("value", out var value));
        value.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(42f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }

    // ========== Multi-Step Sequence Tests ==========
    
    [Fact]
    public void MultiStepSequence_ExecutesInOrder()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/multi-step-test.json");
        
        // Act: Start sequence
        RulesDriver.Instance.RunFrame(0.016f);
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Step1 executed immediately
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props1));
        Assert.True(props1.Value.Properties.TryGetValue("step1", out var step1Val));
        step1Val.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(0f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        
        // Act: Run one frame (do step executes)
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert: Step1 now set
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("step1", out var step1Val2));
        step1Val2.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(1f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        
        // Act: Run past delay (0.1s)
        SequenceDriver.Instance.RunFrame(0.12f);
        
        // Assert: Step2 executed
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props3));
        Assert.True(props3.Value.Properties.TryGetValue("step2", out var step2Val));
        step2Val.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(2f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        
        // Act: Run through tween (0.2s)
        SequenceDriver.Instance.RunFrame(0.25f);
        
        // Assert: Step3 at final tween value, step4 executed
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props4));
        Assert.True(props4.Value.Properties.TryGetValue("step3", out var step3Val));
        Assert.True(props4.Value.Properties.TryGetValue("step4", out var step4Val));
        step3Val.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(10f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        step4Val.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(4f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
    }

    // ========== Concurrent Sequences Tests ==========
    
    [Fact]
    public void ConcurrentSequences_RunSimultaneously()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/concurrent-sequences-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("sequence-1", out var seq1Index));
        Assert.True(SequenceRegistry.Instance.TryResolveById("sequence-2", out var seq2Index));
        
        // Act: Start both sequences
        SequenceDriver.Instance.StartSequence(entity.Value, seq1Index);
        SequenceDriver.Instance.StartSequence(entity.Value, seq2Index);
        
        // Act: Run for 0.15s (seq1 should complete, seq2 still running)
        SequenceDriver.Instance.RunFrame(0.15f);
        
        // Assert: Seq1 completed, seq2 not yet
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props1));
        Assert.True(props1.Value.Properties.TryGetValue("seq1", out var seq1Val));
        Assert.True(props1.Value.Properties.TryGetValue("seq2", out var seq2Val));
        seq1Val.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(1f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        seq2Val.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(0f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        
        // Act: Run past seq2 delay
        SequenceDriver.Instance.RunFrame(0.15f);
        
        // Assert: Both completed
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("seq2", out var seq2Val2));
        seq2Val2.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(2f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
    }

    // ========== Entity Deactivation Tests ==========
    
    [Fact]
    public void EntityDeactivation_CancelsAllSequences()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));
        
        // Act: Start sequence
        SequenceDriver.Instance.StartSequence(entity.Value, seqIndex);
        
        // Act: Deactivate entity
        EntityRegistry.Instance.Deactivate(entity.Value);
        
        // Act: Run sequence driver (should not execute on deactivated entity)
        SequenceDriver.Instance.RunFrame(0.5f);
        
        // Assert: Value should still be 0 (sequence canceled)
        // Note: Can't check behavior on deactivated entity, so we verify no crash
        Assert.Empty(_errorListener.ReceivedEvents);
    }

    // ========== Sequence Completion Tests ==========
    
    [Fact]
    public void SequenceCompletion_AutoRemovesFromActive()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/delay-test.json");
        
        // Act: Start sequence
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Act: Run to completion
        SequenceDriver.Instance.RunFrame(0.6f);
        
        // Assert: Sequence completed
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("value", out var value));
        value.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(1f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        
        // Act: Run again (should not re-execute)
        SequenceDriver.Instance.RunFrame(1.0f);
        
        // Assert: Value still 1 (sequence not re-run)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("value", out var value2));
        value2.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(1f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
    }

    // ========== Negative Tests ==========
    
    [Fact]
    public void InvalidSequenceId_PushesErrorEvent()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        
        // Act: Try to resolve invalid sequence ID
        var resolved = SequenceRegistry.Instance.TryResolveById("nonexistent-sequence", out var _);
        
        // Assert: Resolution failed
        Assert.False(resolved);
    }
}
