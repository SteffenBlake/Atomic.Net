using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Ids;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

/// <summary>
/// Integration tests for RulesDriver frame execution system.
/// Tests the full pipeline: Scene → Rules → JsonLogic → Mutations → Entity Updates
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
        // Clean up ALL entities (both global and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void RunFrame_WithSumAggregate_AppliesCorrectTotalEnemyHealth()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/Aggregates/sum-total-enemy-health.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.016667f);

        // Assert
        // senior-dev: All 3 enemies should have totalEnemyHealth = 350 (100 + 200 + 50)
        Assert.True(EntityIdRegistry.Instance.TryResolve("enemy1", out var enemy1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("enemy2", out var enemy2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("enemy3", out var enemy3));

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(enemy1.Value, out var props1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(enemy2.Value, out var props2));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(enemy3.Value, out var props3));

        Assert.True(props1.Value.Properties!.TryGetValue("totalEnemyHealth", out var total1));
        Assert.True(props2.Value.Properties!.TryGetValue("totalEnemyHealth", out var total2));
        Assert.True(props3.Value.Properties!.TryGetValue("totalEnemyHealth", out var total3));

        // senior-dev: JsonLogic returns decimals, check float values
        Assert.True(total1.TryMatch(out float f1Val) && f1Val == 350f);
        Assert.True(total2.TryMatch(out float f2Val) && f2Val == 350f);
        Assert.True(total3.TryMatch(out float f3Val) && f3Val == 350f);
    }

    [Fact]
    public void RunFrame_WithAverageCalculation_AppliesCorrectAvgPartyHealth()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/Aggregates/average-party-health.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.016667f);

        // Assert
        // senior-dev: All 4 party members should have avgPartyHealth = 70 ((100 + 80 + 60 + 40) / 4)
        Assert.True(EntityIdRegistry.Instance.TryResolve("p1", out var p1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("p2", out var p2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("p3", out var p3));
        Assert.True(EntityIdRegistry.Instance.TryResolve("p4", out var p4));

        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(p1.Value, out var props1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(p2.Value, out var props2));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(p3.Value, out var props3));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(p4.Value, out var props4));

        Assert.True(props1.Value.Properties!.TryGetValue("avgPartyHealth", out var avg1));
        Assert.True(props2.Value.Properties!.TryGetValue("avgPartyHealth", out var avg2));
        Assert.True(props3.Value.Properties!.TryGetValue("avgPartyHealth", out var avg3));
        Assert.True(props4.Value.Properties!.TryGetValue("avgPartyHealth", out var avg4));

        Assert.True(avg1.TryMatch(out float f1Val) && f1Val == 70f);
        Assert.True(avg2.TryMatch(out float f2Val) && f2Val == 70f);
        Assert.True(avg3.TryMatch(out float f3Val) && f3Val == 70f);
        Assert.True(avg4.TryMatch(out float f4Val) && f4Val == 70f);
    }

    [Fact]
    public void RunFrame_WithMinMaxAggregate_AppliesCorrectWeakestAndStrongestHealth()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/Aggregates/min-max-enemy-health.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.016667f);

        // Assert
        // senior-dev: All enemies should have weakestEnemyHealth = 50, strongestEnemyHealth = 250
        Assert.True(EntityIdRegistry.Instance.TryResolve("e1", out var e1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("e2", out var e2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("e3", out var e3));

        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e1.Value, out var props1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e2.Value, out var props2));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e3.Value, out var props3));

        Assert.True(props1.Value.Properties!.TryGetValue("weakestEnemyHealth", out var weak1));
        Assert.True(props2.Value.Properties!.TryGetValue("weakestEnemyHealth", out var weak2));
        Assert.True(props3.Value.Properties!.TryGetValue("weakestEnemyHealth", out var weak3));

        Assert.True(props1.Value.Properties!.TryGetValue("strongestEnemyHealth", out var strong1));
        Assert.True(props2.Value.Properties!.TryGetValue("strongestEnemyHealth", out var strong2));
        Assert.True(props3.Value.Properties!.TryGetValue("strongestEnemyHealth", out var strong3));

        Assert.True(weak1.TryMatch(out float w1Val) && w1Val == 50f);
        Assert.True(weak2.TryMatch(out float w2Val) && w2Val == 50f);
        Assert.True(weak3.TryMatch(out float w3Val) && w3Val == 50f);

        Assert.True(strong1.TryMatch(out float s1Val) && s1Val == 250f);
        Assert.True(strong2.TryMatch(out float s2Val) && s2Val == 250f);
        Assert.True(strong3.TryMatch(out float s3Val) && s3Val == 250f);
    }

    [Fact]
    public void RunFrame_WithDeltaTimeDamageOverTime_AppliesCorrectHealthReduction()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/DeltaTime/damage-over-time.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        // senior-dev: RunFrame with deltaTime = 0.5f, burnDPS = 10, initial health = 100
        // Expected: health = 95 (100 - 10 * 0.5)
        RulesDriver.Instance.RunFrame(0.5f);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        // Resolved via TryResolve above

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var props));
        Assert.True(props.Value.Properties!.TryGetValue("health", out var health));

        Assert.True(health.TryMatch(out float hVal) && hVal == 95f);
    }

    [Fact]
    public void RunFrame_WithConditionalCount_AppliesCorrectNearbyCount()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/Aggregates/conditional-count-nearby.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.016667f);

        // Assert
        // senior-dev: All 4 units should have nearbyCount = 3 (u1, u2, u4 are within 10 units)
        Assert.True(EntityIdRegistry.Instance.TryResolve("u1", out var u1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("u2", out var u2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("u3", out var u3));
        Assert.True(EntityIdRegistry.Instance.TryResolve("u4", out var u4));

        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(u1.Value, out var props1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(u2.Value, out var props2));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(u3.Value, out var props3));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(u4.Value, out var props4));

        Assert.True(props1.Value.Properties!.TryGetValue("nearbyCount", out var count1));
        Assert.True(props2.Value.Properties!.TryGetValue("nearbyCount", out var count2));
        Assert.True(props3.Value.Properties!.TryGetValue("nearbyCount", out var count3));
        Assert.True(props4.Value.Properties!.TryGetValue("nearbyCount", out var count4));

        Assert.True(count1.TryMatch(out float c1Val) && c1Val == 3f);
        Assert.True(count2.TryMatch(out float c2Val) && c2Val == 3f);
        Assert.True(count3.TryMatch(out float c3Val) && c3Val == 3f);
        Assert.True(count4.TryMatch(out float c4Val) && c4Val == 3f);
    }

    [Fact]
    public void RunFrame_WithNestedConditionals_AppliesCorrectHealthRegen()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/Conditionals/nested-health-regen.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        // senior-dev: RunFrame with deltaTime = 0.5f
        // c1 (health 90): healthRegen = 2.5 (deltaTime * 5)
        // c2 (health 60): healthRegen = 5.0 (deltaTime * 10)
        // c3 (health 30): healthRegen = 10.0 (deltaTime * 20)
        RulesDriver.Instance.RunFrame(0.5f);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("c1", out var c1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("c2", out var c2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("c3", out var c3));

        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(c1.Value, out var props1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(c2.Value, out var props2));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(c3.Value, out var props3));

        Assert.True(props1.Value.Properties!.TryGetValue("healthRegen", out var regen1));
        Assert.True(props2.Value.Properties!.TryGetValue("healthRegen", out var regen2));
        Assert.True(props3.Value.Properties!.TryGetValue("healthRegen", out var regen3));

        Assert.True(regen1.TryMatch(out float r1Val) && r1Val == 2.5f);
        Assert.True(regen2.TryMatch(out float r2Val) && r2Val == 5.0f);
        Assert.True(regen3.TryMatch(out float r3Val) && r3Val == 10.0f);
    }

    [Fact]
    public void RunFrame_WithMultiConditionWhere_AppliesAfflictedOnlyToValidEntities()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/Conditionals/multi-condition-where.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.016667f);

        // Assert
        // senior-dev: Only e1, e2, e4 should have afflicted = true (e3 has health 0)
        Assert.True(EntityIdRegistry.Instance.TryResolve("e1", out var e1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("e2", out var e2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("e3", out var e3));
        Assert.True(EntityIdRegistry.Instance.TryResolve("e4", out var e4));

        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e1.Value, out var props1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e2.Value, out var props2));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e3.Value, out var props3));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(e4.Value, out var props4));

        Assert.True(props1.Value.Properties!.TryGetValue("afflicted", out var afflicted1));
        Assert.True(props2.Value.Properties!.TryGetValue("afflicted", out var afflicted2));
        Assert.False(props3.Value.Properties!.ContainsKey("afflicted")); // senior-dev: e3 should NOT have afflicted
        Assert.True(props4.Value.Properties!.TryGetValue("afflicted", out var afflicted4));

        Assert.True(afflicted1.TryMatch(out bool a1Val) && a1Val == true);
        Assert.True(afflicted2.TryMatch(out bool a2Val) && a2Val == true);
        Assert.True(afflicted4.TryMatch(out bool a4Val) && a4Val == true);
    }

    [Fact]
    public void RunFrame_WithTagBasedFiltering_AppliesCorrectEnemyAndAllyCount()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/Aggregates/tag-based-filtering.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        RulesDriver.Instance.RunFrame(0.016667f);

        // Assert
        // senior-dev: All 5 units should have enemyCount = 3, allyCount = 2
        Assert.True(EntityIdRegistry.Instance.TryResolve("u1", out var u1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("u2", out var u2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("u3", out var u3));
        Assert.True(EntityIdRegistry.Instance.TryResolve("u4", out var u4));
        Assert.True(EntityIdRegistry.Instance.TryResolve("u5", out var u5));

        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above
        // Resolved via TryResolve above

        foreach (var unit in new[] { u1.Value, u2.Value, u3.Value, u4.Value, u5.Value })
        {
            Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(unit, out var props));
            Assert.True(props.Value.Properties!.TryGetValue("enemyCount", out var enemyCount));
            Assert.True(props.Value.Properties!.TryGetValue("allyCount", out var allyCount));

            Assert.True(enemyCount.TryMatch(out float ec) && ec == 3f);
            Assert.True(allyCount.TryMatch(out float ac) && ac == 2f);
        }
    }

    [Fact]
    public void RunFrame_WithDeltaTimeZero_NoOpMutation()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/DeltaTime/damage-over-time.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        // senior-dev: RunFrame with deltaTime = 0, should result in no damage
        RulesDriver.Instance.RunFrame(0f);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        // Resolved via TryResolve above

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var props));
        Assert.True(props.Value.Properties!.TryGetValue("health", out var health));

        // senior-dev: Health should remain 100 (no damage taken)
        Assert.True(health.TryMatch(out float hVal) && hVal == 100f);
    }

    [Fact]
    public void RunFrame_MultipleCallsInSequence_StateStacks()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/DeltaTime/damage-over-time.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Act
        // senior-dev: Call RunFrame multiple times to verify state stacks
        RulesDriver.Instance.RunFrame(0.5f); // health: 100 - 5 = 95
        RulesDriver.Instance.RunFrame(0.5f); // health: 95 - 5 = 90
        RulesDriver.Instance.RunFrame(0.5f); // health: 90 - 5 = 85

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        // Resolved via TryResolve above

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var props));
        Assert.True(props.Value.Properties!.TryGetValue("health", out var health));

        Assert.True(health.TryMatch(out float hVal) && hVal == 85f);
    }
}
