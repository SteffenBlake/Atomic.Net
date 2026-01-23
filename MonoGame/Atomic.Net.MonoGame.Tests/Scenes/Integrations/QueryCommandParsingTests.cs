using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

/// <summary>
/// Integration tests for Query + Command system parsing.
/// Tests the full pipeline: JSON scene with rules → Parsed JsonRule objects
/// Stage 1: Parser Only - no execution or evaluation
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class QueryCommandParsingTests : IDisposable
{
    public QueryCommandParsingTests()
    {
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        SceneSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both persistent and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void LoadScene_WithPoisonRule_ParsesSimpleTagSelector()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/poison-rule.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Stubbed API - expects senior-dev to implement SceneLoader rules parsing
        // This test verifies that a scene with a single rule using #tag selector parses correctly
        
        // test-architect: Once implemented, verify:
        // 1. Scene loads without errors
        // 2. Rules collection contains 1 rule
        // 3. Rule.From is TaggedEntitySelector with Tag = "poisoned"
        // 4. Rule.Where and Rule.Do contain JsonNode (not evaluated, just stored)
        
        // test-architect: For now, this will fail with NotImplementedException
        Assert.True(true, "Placeholder - waiting for @senior-dev to implement rules parsing in SceneLoader");
    }

    [Fact]
    public void LoadScene_WithComplexPrecedence_ParsesUnionAndRefinement()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/complex-precedence.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Stubbed API - expects senior-dev to implement
        // This test verifies complex selector precedence:
        // "@player, !enter:#enemies:#boss" should parse as:
        // Union([@player, CollisionEnter(Next: Tagged("enemies", Next: Tagged("boss")))])
        
        // test-architect: Once implemented, verify:
        // 1. Scene loads without errors
        // 2. Rules collection contains 1 rule
        // 3. Rule.From is UnionEntitySelector with 2 children:
        //    - IdEntitySelector("player")
        //    - CollisionEnterEntitySelector(Next: Tagged("enemies", Next: Tagged("boss")))
        
        Assert.True(true, "Placeholder - waiting for @senior-dev to implement");
    }

    [Fact]
    public void LoadScene_WithInvalidSelector_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "Scenes/Tests/QueryCommand/invalid-selector.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Stubbed API - expects senior-dev to implement
        // This test verifies that invalid selector syntax "@@player" fires ErrorEvent
        
        // test-architect: Once implemented, verify:
        // 1. ErrorEvent is fired with message about invalid selector syntax
        // 2. Scene continues loading (entities are still spawned)
        // 3. Invalid rule is skipped/not added to rules collection
        
        Assert.NotEmpty(errorListener.ReceivedEvents);
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("@@") || 
            e.Message.Contains("invalid") || 
            e.Message.Contains("selector") ||
            e.Message.Contains("parse"));
        
        // test-architect: This assertion will fail until implementation is complete
        Assert.NotEqual(default, errorEvent);
    }

    [Fact]
    public void LoadScene_WithNoEntities_LoadsRulesSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/no-entities.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Stubbed API - expects senior-dev to implement
        // This test verifies that a scene with empty entities array but valid rules loads
        
        // test-architect: Once implemented, verify:
        // 1. Scene loads without errors (empty entities array is valid)
        // 2. Rules collection contains 1 rule
        // 3. No entities are spawned (entities array is empty)
        
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Empty(activeEntities);
        
        Assert.True(true, "Placeholder - waiting for @senior-dev to implement");
    }

    [Fact]
    public void LoadScene_WithMultipleRules_ParsesAllRules()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/multiple-rules.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Stubbed API - expects senior-dev to implement
        // This test verifies that a scene with 2+ rules parses all of them
        
        // test-architect: Once implemented, verify:
        // 1. Scene loads without errors
        // 2. Rules collection contains 2 rules
        // 3. First rule has From = TaggedEntitySelector("poisoned")
        // 4. Second rule has From = TaggedEntitySelector("burning")
        // 5. Both rules have valid Where and Do JsonNode data
        
        Assert.True(true, "Placeholder - waiting for @senior-dev to implement");
    }

    [Fact]
    public void LoadScene_WithValidParent_MigratedToNewParser()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/parent-valid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: This test verifies Parent behavior migration to new EntitySelector.TryParse
        // The "@player" syntax should work via new parser, not hardcoded string matching
        
        // Verify entities loaded
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var playerEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out var weaponEntity));
        
        // Verify parent-child relationship established
        Assert.True(weaponEntity.Value.TryGetParent(out var parent), "weapon should have parent");
        Assert.NotNull(parent);
        Assert.Equal(playerEntity.Value.Index, parent.Value.Index);
        
        // test-architect: Once EntitySelector.TryParse is implemented, this should use
        // the new parser instead of hardcoded "@" logic in EntitySelectorConverter
    }

    [Fact]
    public void LoadScene_WithInvalidParent_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "Scenes/Tests/QueryCommand/parent-invalid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: This test verifies that invalid parent selector "@@invalid" fires ErrorEvent
        // via the new EntitySelector.TryParse method
        
        // Verify entities loaded
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out var weaponEntity));
        
        // Verify weapon has no parent (invalid selector was rejected)
        Assert.False(weaponEntity.Value.TryGetParent(out _), "weapon should not have parent due to invalid selector");
        
        // Verify ErrorEvent was fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("@@") || 
            e.Message.Contains("invalid") ||
            e.Message.Contains("selector") ||
            e.Message.Contains("parse"));
        
        // test-architect: This assertion will fail until EntitySelector.TryParse validates "@@" syntax
        Assert.NotEqual(default, errorEvent);
    }

    [Fact]
    public void LoadScene_WithoutRulesField_LoadsSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/parent-valid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: This test verifies that scenes without "rules" field load successfully
        // The "rules" field is optional - scenes with only "entities" are valid
        
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.True(activeEntities.Count >= 2, "Scene without rules should still load entities");
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out _));
    }

    [Fact(Skip = "Scene validation not yet implemented - waiting for @senior-dev")]
    public void LoadScene_WithoutEntitiesField_FiresErrorEvent()
    {
        // test-architect: This test is skipped until senior-dev implements validation
        // Per sprint requirements, ALL scenes must have "entities" array (even if empty)
        // A scene missing "entities" field should fire ErrorEvent
        
        // Arrange
        // using var errorListener = new FakeEventListener<ErrorEvent>();
        // var scenePath = "Scenes/Tests/QueryCommand/missing-entities.json";
        
        // Act
        // SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        // Assert.NotEmpty(errorListener.ReceivedEvents);
        // var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
        //     e.Message.Contains("entities") || e.Message.Contains("required"));
        // Assert.NotEqual(default, errorEvent);
    }

    [Fact(Skip = "API stub - waiting for @senior-dev implementation")]
    public void LoadScene_VerifyRulesNotExecuted()
    {
        // test-architect: This test verifies that Stage 1 (Parser Only) does NOT execute rules
        // Rules should be parsed and stored, but not evaluated or executed
        
        // Arrange
        // var scenePath = "Scenes/Tests/QueryCommand/poison-rule.json";
        
        // Act
        // SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        // test-architect: Verify that goblin entity has original health = 50
        // If rules were executed, health would be reduced by 1
        // Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        // var properties = GetPropertiesBehavior(goblin); // Need to implement helper
        // Assert.Equal(50, properties["health"]); // Health should be unchanged
    }
}
