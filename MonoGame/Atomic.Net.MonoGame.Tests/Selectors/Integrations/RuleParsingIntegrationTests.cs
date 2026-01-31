using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Tests.Selectors.Integrations;

/// <summary>
/// Integration tests for rule loading from JSON scene files.
/// Tests full pipeline: JSON → Rules → RuleRegistry → Partition allocation → Events
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class RuleParsingIntegrationTests : IDisposable
{
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public RuleParsingIntegrationTests()
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
    public void LoadGameScene_WithPoisonRule_LoadsRuleCorrectly()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/poison-rule.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify exactly 1 rule was loaded
        var rules = RuleRegistry.Instance.Rules.ToList();
        Assert.Single(rules);
        
        var rule = rules[0].Value;
        
        // test-architect: Verify rule is in scene partition (index >= MaxGlobalRules)
        Assert.True(rules[0].Index >= Constants.MaxGlobalRules, 
            $"Rule should be in scene partition (>= {Constants.MaxGlobalRules})");
        
        // test-architect: Verify FROM selector is #poisoned
        Assert.True(rule.From.TryMatch(out TagEntitySelector? tagSelector), "From should be TagEntitySelector");
        Assert.NotNull(tagSelector);
        Assert.Equal("#poisoned", rule.From.ToString());
        
        // test-architect: Verify WHERE clause is present (JsonNode)
        Assert.NotNull(rule.Where);
        
        // test-architect: Verify DO command is MutCommand
        Assert.True(rule.Do.TryMatch(out MutCommand mutCommand), "Do should be MutCommand");
        Assert.NotNull(mutCommand.Mut);
        Assert.NotEmpty(mutCommand.Mut);
    }

    [Fact]
    public void LoadGameScene_WithComplexPrecedence_LoadsRuleCorrectly()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/complex-precedence.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify exactly 1 rule was loaded
        var rules = RuleRegistry.Instance.Rules.ToList();
        Assert.Single(rules);
        
        var rule = rules[0].Value;
        
        // test-architect: Verify FROM selector is union: @player, !enter:#enemies:#boss
        Assert.True(rule.From.TryMatch(out UnionEntitySelector? unionSelector), "From should be UnionEntitySelector");
        Assert.NotNull(unionSelector);
        // test-architect: Verify ToString shows correct precedence
        var selectorStr = rule.From.ToString();
        Assert.Contains("@player", selectorStr);
        Assert.Contains("!enter", selectorStr);
        Assert.Contains("#enemies", selectorStr);
        Assert.Contains("#boss", selectorStr);
    }

    [Fact]
    public void LoadGameScene_WithInvalidSelector_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/invalid-selector.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should fire ErrorEvent for invalid selector syntax (@@player)
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent for invalid selector");
        
        // test-architect: Error should mention the invalid selector or JSON parsing failure
        var hasRelevantError = _errorListener.ReceivedEvents.Any(e => 
            e.Message.Contains("@@") || 
            e.Message.Contains("selector", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("parse", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("JSON", StringComparison.OrdinalIgnoreCase)
        );
        Assert.True(hasRelevantError, "Error should mention selector parsing or JSON failure");
    }

    [Fact]
    public void LoadGameScene_WithNoEntitiesButRules_LoadsRuleCorrectly()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/no-entities-with-rules.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify exactly 1 rule was loaded (empty entities array is valid)
        var rules = RuleRegistry.Instance.Rules.ToList();
        Assert.Single(rules);
        
        var rule = rules[0].Value;
        
        // test-architect: Verify FROM selector is #poisoned
        Assert.True(rule.From.TryMatch(out TagEntitySelector? tagSelector), "From should be TagEntitySelector");
        Assert.NotNull(tagSelector);
        Assert.Equal("#poisoned", rule.From.ToString());
    }

    [Fact]
    public void LoadGameScene_WithMultipleRules_LoadsAllRulesCorrectly()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/multiple-rules.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify exactly 2 rules were loaded
        var rules = RuleRegistry.Instance.Rules.ToList();
        Assert.Equal(2, rules.Count);
        
        // test-architect: Verify both rules are in scene partition
        Assert.All(rules, rule => 
            Assert.True(rule.Index >= Constants.MaxGlobalRules, 
                $"Rule {rule.Index} should be in scene partition"));
        
        // test-architect: Verify rules have correct selectors (#poisoned and #burning)
        var selector1 = rules[0].Value.From.ToString();
        var selector2 = rules[1].Value.From.ToString();
        Assert.True(selector1 == "#poisoned" || selector1 == "#burning", "First rule should be #poisoned or #burning");
        Assert.True(selector2 == "#poisoned" || selector2 == "#burning", "Second rule should be #poisoned or #burning");
        Assert.NotEqual(selector1, selector2);
    }

    [Fact]
    public void LoadGameScene_WithParentValidSelector_LoadsSuccessfully()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/parent-valid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should load successfully with no errors (parent: "@player" is valid)
        // Note: This scene has no rules, just entities with parent selectors
        Assert.Empty(_errorListener.ReceivedEvents);
    }

    [Fact]
    public void LoadGameScene_WithParentInvalidSelector_FiresErrorEvent()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/parent-invalid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should fire ErrorEvent for invalid parent selector (@@invalid)
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent for invalid parent selector");
        
        // test-architect: Error should mention the invalid selector or parsing failure
        var hasRelevantError = _errorListener.ReceivedEvents.Any(e => 
            e.Message.Contains("@@") || 
            e.Message.Contains("selector", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("parse", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("JSON", StringComparison.OrdinalIgnoreCase)
        );
        Assert.True(hasRelevantError, "Error should mention selector parsing or JSON failure");
    }

    [Fact]
    public void LoadGameScene_WithEntitiesOnly_LoadsSuccessfully()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/entities-only.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should load successfully with no errors (rules are optional)
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // test-architect: No rules should be loaded (scene has no rules section)
        var rules = RuleRegistry.Instance.Rules.ToList();
        Assert.Empty(rules);
    }

    [Fact]
    public void LoadGlobalScene_WithRules_LoadsIntoGlobalPartition()
    {
        // Arrange
        var scenePath = "Selectors/Fixtures/poison-rule.json";

        // Act
        SceneLoader.Instance.LoadGlobalScene(scenePath);

        // Assert
        // test-architect: Verify exactly 1 rule was loaded
        var rules = RuleRegistry.Instance.Rules.ToList();
        Assert.Single(rules);
        
        var rule = rules[0].Value;
        
        // test-architect: Verify rule is in GLOBAL partition (index < MaxGlobalRules)
        Assert.True(rules[0].Index < Constants.MaxGlobalRules, 
            $"Rule should be in global partition (< {Constants.MaxGlobalRules})");
    }

    [Fact]
    public void ResetEvent_ClearsSceneRulesOnly()
    {
        // Arrange
        var globalScenePath = "Selectors/Fixtures/poison-rule.json";
        var sceneScenePath = "Selectors/Fixtures/multiple-rules.json";
        
        SceneLoader.Instance.LoadGlobalScene(globalScenePath);
        SceneLoader.Instance.LoadGameScene(sceneScenePath);
        
        // test-architect: Verify we have 3 total rules (1 global + 2 scene)
        var rulesBeforeReset = RuleRegistry.Instance.Rules.ToList();
        Assert.Equal(3, rulesBeforeReset.Count);

        // Act
        EventBus<ResetEvent>.Push(new());

        // Assert
        // test-architect: After reset, only global rule should remain
        var rulesAfterReset = RuleRegistry.Instance.Rules.ToList();
        Assert.Single(rulesAfterReset);
        
        // test-architect: Remaining rule should be in global partition
        Assert.True(rulesAfterReset[0].Index < Constants.MaxGlobalRules, 
            "Remaining rule should be in global partition");
    }

    [Fact]
    public void ShutdownEvent_ClearsAllRules()
    {
        // Arrange
        var globalScenePath = "Selectors/Fixtures/poison-rule.json";
        var sceneScenePath = "Selectors/Fixtures/multiple-rules.json";
        
        SceneLoader.Instance.LoadGlobalScene(globalScenePath);
        SceneLoader.Instance.LoadGameScene(sceneScenePath);
        
        // test-architect: Verify we have 3 total rules (1 global + 2 scene)
        var rulesBeforeShutdown = RuleRegistry.Instance.Rules.ToList();
        Assert.Equal(3, rulesBeforeShutdown.Count);

        // Act
        EventBus<ShutdownEvent>.Push(new());

        // Assert
        // test-architect: After shutdown, all rules should be cleared
        var rulesAfterShutdown = RuleRegistry.Instance.Rules.ToList();
        Assert.Empty(rulesAfterShutdown);
    }

    [Fact]
    public void LoadGameScene_MultipleScenes_AccumulatesRulesInRegistry()
    {
        // Arrange
        var scene1Path = "Selectors/Fixtures/poison-rule.json";
        var scene2Path = "Selectors/Fixtures/multiple-rules.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scene1Path);
        var rulesAfterFirstScene = RuleRegistry.Instance.Rules.ToList();
        
        EventBus<ResetEvent>.Push(new()); // Clear scene rules
        
        SceneLoader.Instance.LoadGameScene(scene2Path);
        var rulesAfterSecondScene = RuleRegistry.Instance.Rules.ToList();

        // Assert
        // test-architect: First scene should load 1 rule
        Assert.Single(rulesAfterFirstScene);
        
        // test-architect: After reset and loading second scene, should have 2 rules (from second scene only)
        Assert.Equal(2, rulesAfterSecondScene.Count);
    }
}
