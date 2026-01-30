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
}
