using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Sequencing;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Sequencing.Integrations;

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
    public void InvalidSequenceId_ReturnsF false()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/delay-test.json");
        
        // Act & Assert: Try to resolve invalid sequence ID
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
        SequenceDriver.Instance.StopSequence(entity.Value, seqIndex);
        
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
        SequenceDriver.Instance.ResetSequence(entity.Value, seqIndex);
        
        // Assert: No error (graceful no-op)
        Assert.Empty(_errorListener.ReceivedEvents);
    }
}
