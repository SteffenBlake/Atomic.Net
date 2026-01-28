using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

/// <summary>
/// Integration tests for RulesDriver execution.
/// Tests the full pipeline: Selector → WHERE → DO → Mutation
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class RulesDriverIntegrationTests : IDisposable
{
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public RulesDriverIntegrationTests()
    {
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        // Clean up between tests
        _errorListener.Dispose();
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void RunFrame_WithSimplePropertyMutation_AppliesMutationCorrectly()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/poison-rule.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert
        // senior-dev: Check for any errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // senior-dev: Verify goblin health decreased from 50 to 49 (poison-rule.json has poisonStacks: 3, but WHERE filters for >0, DO subtracts 1)
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var props));
        Assert.NotNull(props.Value.Properties);
        
        // senior-dev: health should be 49 (50 - 1)
        Assert.True(props.Value.Properties.TryGetValue("health", out var healthValue));
        healthValue.Visit(
            s => Assert.Fail("Expected float, got string"),
            f => Assert.Equal(49f, f),
            b => Assert.Fail("Expected float, got bool"),
            () => Assert.Fail("Expected float, got empty")
        );
    }

    [Fact]
    public void RunFrame_WithDeltaTimeMutation_AppliesDeltaTimeCorrectly()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/burn-damage-rule.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Act
        RulesDriver.Instance.RunFrame(0.5f);
        
        // Assert
        // senior-dev: Verify goblin health decreased from 100 to 95 (100 - 10 * 0.5)
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var props));
        Assert.NotNull(props.Value.Properties);
        
        Assert.True(props.Value.Properties.TryGetValue("health", out var healthValue));
        healthValue.Visit(
            s => Assert.Fail("Expected float, got string"),
            f => Assert.Equal(95f, f),
            b => Assert.Fail("Expected float, got bool"),
            () => Assert.Fail("Expected float, got empty")
        );
    }

    [Fact]
    public void RunFrame_WithSumAggregate_CalculatesTotalCorrectly()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/sum-aggregate-rule.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert
        // senior-dev: All three enemies should have totalEnemyHealth = 350 (100 + 200 + 50)
        Assert.True(EntityIdRegistry.Instance.TryResolve("enemy1", out var enemy1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("enemy2", out var enemy2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("enemy3", out var enemy3));
        
        // senior-dev: Check enemy1
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(enemy1.Value, out var props1));
        Assert.NotNull(props1.Value.Properties);
        Assert.True(props1.Value.Properties.TryGetValue("totalEnemyHealth", out var total1));
        total1.Visit(
            s => Assert.Fail("Expected float, got string"),
            f => Assert.Equal(350f, f),
            b => Assert.Fail("Expected float, got bool"),
            () => Assert.Fail("Expected float, got empty")
        );
        
        // senior-dev: Check enemy2
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(enemy2.Value, out var props2));
        Assert.NotNull(props2.Value.Properties);
        Assert.True(props2.Value.Properties.TryGetValue("totalEnemyHealth", out var total2));
        total2.Visit(
            s => Assert.Fail("Expected float, got string"),
            f => Assert.Equal(350f, f),
            b => Assert.Fail("Expected float, got bool"),
            () => Assert.Fail("Expected float, got empty")
        );
        
        // senior-dev: Check enemy3
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(enemy3.Value, out var props3));
        Assert.NotNull(props3.Value.Properties);
        Assert.True(props3.Value.Properties.TryGetValue("totalEnemyHealth", out var total3));
        total3.Visit(
            s => Assert.Fail("Expected float, got string"),
            f => Assert.Equal(350f, f),
            b => Assert.Fail("Expected float, got bool"),
            () => Assert.Fail("Expected float, got empty")
        );
    }

    [Fact]
    public void RunFrame_WithMultipleRules_ExecutesSequentially()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/multiple-rules.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        var initialHealth = 50f;
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var initialProps));
        Assert.NotNull(initialProps.Value.Properties);
        Assert.True(initialProps.Value.Properties.TryGetValue("health", out var initialHealthValue));
        initialHealthValue.Visit(
            s => Assert.Fail("Expected float"),
            f => initialHealth = f,
            b => Assert.Fail("Expected float"),
            () => Assert.Fail("Expected float")
        );
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert
        // senior-dev: multiple-rules.json has poisonStacks: 2 and burnStacks: 1
        // First rule (#poisoned): health -= poisonStacks (50 - 2 = 48)
        // Second rule (#burning): health -= burnStacks * 2 (48 - 2 = 46)
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var props));
        Assert.NotNull(props.Value.Properties);
        Assert.True(props.Value.Properties.TryGetValue("health", out var healthValue));
        healthValue.Visit(
            s => Assert.Fail("Expected float, got string"),
            f => Assert.Equal(46f, f),
            b => Assert.Fail("Expected float, got bool"),
            () => Assert.Fail("Expected float, got empty")
        );
    }

    [Fact]
    public void RunFrame_WithNoMatchingEntities_DoesNotCrash()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/no-entities-with-rules.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Act & Assert
        // senior-dev: Should complete without errors even though no entities match #poisoned
        RulesDriver.Instance.RunFrame(0.016f);
        
        // senior-dev: No errors should be logged
        Assert.Empty(_errorListener.ReceivedEvents);
    }

    [Fact]
    public void RunFrame_WithNoRules_DoesNotCrash()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/entities-only.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Act & Assert
        // senior-dev: Should complete without errors even though scene has no rules
        RulesDriver.Instance.RunFrame(0.016f);
        
        // senior-dev: No errors should be logged
        Assert.Empty(_errorListener.ReceivedEvents);
    }

    [Fact]
    public void RunFrame_CalledMultipleTimes_AccumulatesState()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/poison-rule.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f); // health: 50 - 1 = 49
        RulesDriver.Instance.RunFrame(0.016f); // health: 49 - 1 = 48
        RulesDriver.Instance.RunFrame(0.016f); // health: 48 - 1 = 47
        
        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var props));
        Assert.NotNull(props.Value.Properties);
        Assert.True(props.Value.Properties.TryGetValue("health", out var healthValue));
        healthValue.Visit(
            s => Assert.Fail("Expected float, got string"),
            f => Assert.Equal(47f, f),
            b => Assert.Fail("Expected float, got bool"),
            () => Assert.Fail("Expected float, got empty")
        );
    }
}
