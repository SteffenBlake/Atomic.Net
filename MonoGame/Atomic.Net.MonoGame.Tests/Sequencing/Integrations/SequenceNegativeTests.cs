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
/// Negative test cases for sequence system error handling.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class SequenceNegativeTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public SequenceNegativeTests(ITestOutputHelper output)
    {
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

    [Fact]
    public void InvalidSequenceId_ReturnsFalse()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/delay-test.json");

        // Act & Assert
        Assert.False(SequenceRegistry.Instance.TryResolveById("nonexistent-sequence", out var _));
    }

    [Fact]
    public void StopNonRunningSequence_NoError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));

        // Act: Stop a sequence that was never started
        SequenceDriver.Instance.StopSequence(entity.Value.Index, seqIndex);

        // Assert: No error (graceful no-op)
        Assert.Empty(_errorListener.ReceivedEvents);
    }

    [Fact]
    public void ResetNonRunningSequence_NoError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));

        // Act: Reset a sequence that was never started
        SequenceDriver.Instance.ResetSequence(entity.Value.Index, seqIndex);

        // Assert: No error (graceful no-op)
        Assert.Empty(_errorListener.ReceivedEvents);
    }

    [Fact]
    public void StartSequenceTwice_IsNoOp()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));

        // Act: Start sequence twice (second start should be no-op)
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seqIndex);
        SequenceDriver.Instance.RunFrame(0.05f);
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seqIndex); // No-op
        SequenceDriver.Instance.RunFrame(0.15f);

        // Assert: Sequence completed normally (second start had no effect)
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
    public void StartSequence_InvalidId_ErrorEvent()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/error-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        _errorListener.Clear();

        // Act: Try to start nonexistent sequence
        SequenceStartCmdDriver.Instance.Execute(entity.Value.Index, "invalid-id");

        // Assert: Error event pushed
        Assert.Single(_errorListener.ReceivedEvents);
        Assert.Contains("not found", _errorListener.ReceivedEvents[0].Message);
    }

    [Fact]
    public void StopSequence_InvalidId_ErrorEvent()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/error-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        _errorListener.Clear();

        // Act: Try to stop nonexistent sequence
        SequenceStopCmdDriver.Instance.Execute(entity.Value.Index, "invalid-id");

        // Assert: Error event pushed
        Assert.Single(_errorListener.ReceivedEvents);
        Assert.Contains("not found", _errorListener.ReceivedEvents[0].Message);
    }

    [Fact]
    public void ResetSequence_InvalidId_ErrorEvent()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/error-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        _errorListener.Clear();

        // Act: Try to reset nonexistent sequence
        SequenceResetCmdDriver.Instance.Execute(entity.Value.Index, "invalid-id");

        // Assert: Error event pushed
        Assert.Single(_errorListener.ReceivedEvents);
        Assert.Contains("not found", _errorListener.ReceivedEvents[0].Message);
    }

    [Fact]
    public void MalformedDelayStep_MissingDuration_LogsErrorAndNotRegistered()
    {
        // Arrange
        _errorListener.Clear();
        var sequenceCountBefore = SequenceRegistry.Instance.Sequences.Count;

        // Act: Load scene with malformed delay step (missing duration field)
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/malformed-delay-missing-duration.json");

        // Assert: Error event logged
        Assert.NotEmpty(_errorListener.ReceivedEvents);

        // Assert: Malformed sequence NOT added to registry
        Assert.False(SequenceRegistry.Instance.TryResolveById("malformed-delay", out _));
    }

    [Fact]
    public void MalformedTweenStep_MissingRequiredFields_LogsErrorAndNotRegistered()
    {
        // Arrange
        _errorListener.Clear();

        // Act: Load scene with malformed tween step (missing 'to' and 'do' fields)
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/malformed-tween-missing-fields.json");

        // Assert: Error event logged
        Assert.NotEmpty(_errorListener.ReceivedEvents);

        // Assert: Malformed sequence NOT added to registry
        Assert.False(SequenceRegistry.Instance.TryResolveById("malformed-tween", out _));
    }

    [Fact]
    public void MalformedDoStep_MissingCommand_LogsErrorAndNotRegistered()
    {
        // Arrange
        _errorListener.Clear();

        // Act: Load scene with malformed do step (empty command object)
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/malformed-do-missing-command.json");

        // Assert: Error event logged
        Assert.NotEmpty(_errorListener.ReceivedEvents);

        // Assert: Malformed sequence NOT added to registry
        Assert.False(SequenceRegistry.Instance.TryResolveById("malformed-do", out _));
    }

    [Fact]
    public void MalformedRepeatStep_MissingUntilCondition_LogsErrorAndNotRegistered()
    {
        // Arrange
        _errorListener.Clear();

        // Act: Load scene with malformed repeat step (missing 'until' and 'do' fields)
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/malformed-repeat-missing-until.json");

        // Assert: Error event logged
        Assert.NotEmpty(_errorListener.ReceivedEvents);

        // Assert: Malformed sequence NOT added to registry
        Assert.False(SequenceRegistry.Instance.TryResolveById("malformed-repeat", out _));
    }

    [Fact]
    public void UnknownStepType_LogsErrorAndNotRegistered()
    {
        // Arrange
        _errorListener.Clear();

        // Act: Load scene with unknown step type
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/malformed-unknown-step-type.json");

        // Assert: Error event logged
        Assert.NotEmpty(_errorListener.ReceivedEvents);

        // Assert: Malformed sequence NOT added to registry
        Assert.False(SequenceRegistry.Instance.TryResolveById("unknown-step", out _));
    }

    [Fact]
    public void SequenceMissingId_LogsErrorAndNotRegistered()
    {
        // Arrange
        _errorListener.Clear();
        var sequenceCountBefore = SequenceRegistry.Instance.Sequences.Count;

        // Act: Load scene with sequence missing ID field
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/malformed-sequence-missing-id.json");

        // Assert: Error event logged
        Assert.NotEmpty(_errorListener.ReceivedEvents);

        // Assert: No new sequences added (count unchanged)
        Assert.Equal(sequenceCountBefore, SequenceRegistry.Instance.Sequences.Count);
    }

    [Fact]
    public void DuplicateSequenceId_SceneFileRejected()
    {
        // Arrange
        _errorListener.Clear();

        // Act: Load scene with duplicate sequence IDs
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/malformed-sequence-duplicate-id.json");

        // Assert: Error event logged for duplicate
        Assert.NotEmpty(_errorListener.ReceivedEvents);

        // Assert: First sequence registered, second rejected
        Assert.False(SequenceRegistry.Instance.TryResolveById("duplicate", out var seqIndex));

        // Assert: Only one sequence with this ID exists (by checking steps count matches first)
        Assert.False(SequenceRegistry.Instance.Sequences.TryGetValue(seqIndex, out _));
    }
}
