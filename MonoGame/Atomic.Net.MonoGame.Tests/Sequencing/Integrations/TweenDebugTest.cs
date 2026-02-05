using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Sequencing;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Sequencing.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class TweenDebugTest : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public TweenDebugTest(ITestOutputHelper output)
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
    public void TweenStep_WithHardcodedValue_Works()
    {
        SceneLoader.Instance.LoadGameScene("Sequencing/Fixtures/tween-simple-test.json");
        RulesDriver.Instance.RunFrame(0.016f);
        Assert.Empty(_errorListener.ReceivedEvents);

        // Run for 0.5s - should execute tween step multiple times
        SequenceDriver.Instance.RunFrame(0.5f);

        // Assert: Value should be 99 (hardcoded in mutation)
        Assert.True(EntityIdRegistry.Instance.TryResolve("testEntity", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.True(props.Value.Properties.TryGetValue("value", out var value));
        value.Visit(
            s => Assert.Fail("Expected float"),
            f => Assert.Equal(99f, f),
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected value")
        );
    }
}
