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
    public void StartSequenceTwice_RestartsFromBeginning()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/sequence-commands-test.json");
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(SequenceRegistry.Instance.TryResolveById("test-sequence", out var seqIndex));
        
        // Act: Start sequence, run partially, start again
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seqIndex);
        SequenceDriver.Instance.RunFrame(0.05f);
        SequenceDriver.Instance.StartSequence(entity.Value.Index, seqIndex); // Start again
        SequenceDriver.Instance.RunFrame(0.15f);
        
        // Assert: Sequence completed (restarted from beginning)
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
}
