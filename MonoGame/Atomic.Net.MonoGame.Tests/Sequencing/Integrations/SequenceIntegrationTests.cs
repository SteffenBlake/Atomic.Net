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
/// Integration tests for basic sequence functionality.
/// Tests delay steps, do steps, tween steps, and sequence commands.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class SequenceIntegrationTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public SequenceIntegrationTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        // Clean up between tests
        _errorListener.Dispose();
        _errorLogger.Dispose();

        EventBus<ShutdownEvent>.Push(new());
    }

    // ========== Delay Step Tests ==========
    
    [Fact]
    public void DelayStep_WaitsCorrectDuration_ThenExecutes()
    {
        // Arrange: Load fixture with delay sequence
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/delay-test.json");
        
        // Act: Start sequence via rule
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Sequence started, no errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Act: Run for less than delay duration
        SequenceDriver.Instance.RunFrame(0.3f);
        
        // Assert: Value still 0 (do step hasn't executed)
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("value", out var value1));
        value1.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(0f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
        
        // Act: Run past delay duration (0.3 + 0.3 = 0.6 > 0.5)
        SequenceDriver.Instance.RunFrame(0.3f);
        
        // Assert: Value now 1 (do step executed)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("value", out var value2));
        value2.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(1f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }
    
    // ========== Tween Step Tests ==========
    
    [Fact]
    public void TweenStep_InterpolatesCorrectly_OverDuration()
    {
        // Arrange: Load fixture with tween sequence
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/tween-test.json");
        
        // Act: Start sequence via rule
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Sequence started, no errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Act: Run for half duration
        SequenceDriver.Instance.RunFrame(0.5f);
        
        // Assert: Value approximately 50 (halfway between 0 and 100)
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props1));
        Assert.True(props1.Value.Properties.TryGetValue("value", out var value1));
        value1.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.InRange(f, 45f, 55f), // Allow some tolerance
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
        
        // Act: Complete the tween
        SequenceDriver.Instance.RunFrame(0.6f);
        
        // Assert: Value is 100 (final value)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("value", out var value2));
        value2.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(100f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }
    
    // ========== Repeat Step Tests ==========
    
    [Fact]
    public void RepeatStep_ExecutesAtIntervals_UntilConditionMet()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/repeat-test.json");
        
        // Act: Start sequence
        RulesDriver.Instance.RunFrame(0.016f);
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Act: Run for 0.55s (should trigger 5 times: at 0.1, 0.2, 0.3, 0.4, 0.5)
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
    
    // ========== Sequence Command Tests ==========
    
    [Fact]
    public void SequenceStop_StopsRunningSequence()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));
        
        // Act: Start and run partially through delay
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seqIndex);
        SequenceDriver.Instance.RunFrame(0.05f);
        
        // Act: Stop sequence
        SequenceDriver.Instance.StopSequence(entity.Value.Index, seqIndex);
        
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
    public void SequenceReset_RestartsFromBeginning()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));
        
        // Act: Start and run partially
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seqIndex);
        SequenceDriver.Instance.RunFrame(0.05f);
        
        // Act: Reset sequence
        SequenceDriver.Instance.ResetSequence(entity.Value.Index, seqIndex);
        
        // Act: Run past delay
        SequenceDriver.Instance.RunFrame(0.2f);
        
        // Assert: Sequence completed from beginning
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
    public void MultiStepSequence_ExecutesInOrder_WithTimeCarryover()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/multi-step-test.json");
        
        // Act: Start sequence
        RulesDriver.Instance.RunFrame(0.016f);
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Run one frame for initial do step
        SequenceDriver.Instance.RunFrame(0.016f);
        
        // Assert: Step1 executed
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props1));
        Assert.True(props1.Value.Properties.TryGetValue("step1", out var step1));
        step1.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(1f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        
        // Run past delay + next do step + tween + final do step (all in one call with time carryover)
        SequenceDriver.Instance.RunFrame(0.5f);
        
        // Assert: All steps completed
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props2));
        Assert.True(props2.Value.Properties.TryGetValue("step2", out var step2));
        Assert.True(props2.Value.Properties.TryGetValue("step3", out var step3));
        Assert.True(props2.Value.Properties.TryGetValue("step4", out var step4));
        step2.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(2f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        step3.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(10f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        step4.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(4f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
    }
    
    // ========== Concurrent Sequences Tests ==========
    
    [Fact]
    public void ConcurrentSequences_RunIndependently()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/concurrent-sequences-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("sequence-1", out var seq1));
        Assert.True(SequenceRegistry.Instance.TryResolveById("sequence-2", out var seq2));
        
        // Act: Start both
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seq1);
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seq2);
        
        // Run for 0.15s (seq1 completes at 0.1s, seq2 still running)
        SequenceDriver.Instance.RunFrame(0.15f);
        
        // Assert: seq1 done, seq2 not yet
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props1));
        Assert.True(props1.Value.Properties.TryGetValue("seq1", out var seq1Val));
        Assert.True(props1.Value.Properties.TryGetValue("seq2", out var seq2Val));
        seq1Val.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(1f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        seq2Val.Visit(s => Assert.Fail("Expected float"), f => Assert.Equal(0f, f), b => Assert.Fail("Expected float"), () => Assert.Fail("Expected value"));
        
        // Complete seq2
        SequenceDriver.Instance.RunFrame(0.1f);
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
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seqIndex);
        
        // Act: Deactivate entity
        EntityRegistry.Instance.Deactivate(entity.Value);
        
        // Act: Run driver (should skip deactivated entity)
        SequenceDriver.Instance.RunFrame(0.5f);
        
        // Assert: No crash (sequence was canceled)
        Assert.Empty(_errorListener.ReceivedEvents);
    }
    
    // ========== Large Timestep Tests ==========
    
    [Fact]
    public void RepeatStep_LargeTimestep_ExecutesMultipleTimes()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/large-timestep-test.json");
        
        // Act: Start sequence with large timestep (350ms on 100ms intervals = 3 executions)
        RulesDriver.Instance.RunFrame(0.016f);
        Assert.Empty(_errorListener.ReceivedEvents);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("fast-repeat", out var seqIndex));
        
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seqIndex);
        SequenceDriver.Instance.RunFrame(0.35f); // Should trigger 3 times (at 0.1, 0.2, 0.3)
        
        // Assert: Counter incremented 3 times
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("counter", out var counter));
        counter.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(3f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }
}
