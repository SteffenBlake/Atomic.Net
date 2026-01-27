using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

/// <summary>
/// Integration tests for RulesDriver frame execution.
/// Tests the full pipeline: Scene Loading → Rule Execution → Entity Mutation
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class RulesDriverIntegrationTests : IDisposable
{
    public RulesDriverIntegrationTests()
    {
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities, rules, and state between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void RunFrame_WithSimpleMutation_AppliesPropertyToEntity()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/RulesDriver/simple-mutation.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Single(activeEntities);

        var entity = activeEntities[0];
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(
            entity, out var propertiesRef
        ));

        var properties = propertiesRef.Value.Properties;
        Assert.NotNull(properties);
        Assert.True(properties.ContainsKey("processed"));
        
        // senior-dev: Verify the processed property was set
        var processedValue = properties["processed"];
        Assert.True(processedValue.TryMatch(out string? processedStr));
        Assert.Equal("true", processedStr);
    }

    [Fact]
    public void RunFrame_WithDeltaTimeDamage_CalculatesCorrectHealthReduction()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/RulesDriver/example-4-deltatime-damage.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.5f);

        // Assert
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Single(activeEntities);

        var entity = activeEntities[0];
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(
            entity, out var propertiesRef
        ));

        var properties = propertiesRef.Value.Properties;
        Assert.NotNull(properties);
        Assert.True(properties.ContainsKey("health"));
        
        // senior-dev: Expected: 100 - (10 * 0.5) = 95
        var healthValue = properties["health"];
        Assert.True(healthValue.TryMatch(out float health));
        Assert.Equal(95.0f, health, precision: 2);
    }

    [Fact]
    public void RunFrame_WithSumAggregate_CalculatesTotalEnemyHealth()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/RulesDriver/example-1-sum-aggregate.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Equal(3, activeEntities.Count);

        // senior-dev: All 3 enemies should have totalEnemyHealth = 350 (100 + 200 + 50)
        foreach (var entity in activeEntities)
        {
            Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(
                entity, out var propertiesRef
            ));

            var properties = propertiesRef.Value.Properties;
            Assert.NotNull(properties);
            Assert.True(properties.ContainsKey("totalEnemyHealth"));
            
            var totalHealthValue = properties["totalEnemyHealth"];
            Assert.True(totalHealthValue.TryMatch(out float totalHealth));
            Assert.Equal(350.0f, totalHealth, precision: 2);
        }
    }

    [Fact]
    public void RunFrame_CalledMultipleTimes_MutationsStack()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/RulesDriver/example-4-deltatime-damage.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act - Run frame twice
        RulesDriver.Instance.RunFrame(0.5f);  // First frame: 100 - 5 = 95
        RulesDriver.Instance.RunFrame(0.5f);  // Second frame: 95 - 5 = 90

        // Assert
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        var entity = activeEntities[0];
        
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(
            entity, out var propertiesRef
        ));

        var properties = propertiesRef.Value.Properties;
        var healthValue = properties!["health"];
        Assert.True(healthValue.TryMatch(out float health));
        
        // senior-dev: After 2 frames: 100 - (10 * 0.5) - (10 * 0.5) = 90
        Assert.Equal(90.0f, health, precision: 2);
    }

    [Fact]
    public void RunFrame_WithZeroDeltaTime_AppliesZeroMutation()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/RulesDriver/example-4-deltatime-damage.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.0f);

        // Assert
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        var entity = activeEntities[0];
        
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(
            entity, out var propertiesRef
        ));

        var properties = propertiesRef.Value.Properties;
        var healthValue = properties!["health"];
        Assert.True(healthValue.TryMatch(out float health));
        
        // senior-dev: With deltaTime = 0, health should remain 100
        Assert.Equal(100.0f, health, precision: 2);
    }
}
