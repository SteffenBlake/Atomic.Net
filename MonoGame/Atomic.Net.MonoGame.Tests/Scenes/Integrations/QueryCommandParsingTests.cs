using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

/// <summary>
/// Integration tests for Query + Command system rule parsing.
/// Tests the full pipeline: JSON scene files → Parsed JsonRule objects with validated AST.
/// This is Stage 1: Parser Only - no execution or evaluation.
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
        // Clean up ALL entities between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void LoadGameScene_WithPoisonRule_LoadsEntitiesSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/poison-rule.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify scene loaded (should have 1 entity: goblin)
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Single(activeEntities);
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out _));
    }

    [Fact]
    public void LoadGameScene_WithComplexPrecedence_LoadsEntitiesSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/complex-precedence.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify scene loaded (should have 2 entities: player, boss)
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Equal(2, activeEntities.Count);
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("boss", out _));
    }

    [Fact]
    public void LoadGameScene_WithInvalidSelector_LoadsEntitiesButMayFireErrorEvent()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/invalid-selector.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scene should still load entities even if rule has invalid selector
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out _));
    }

    [Fact]
    public void LoadGameScene_WithNoEntities_LoadsSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/no-entities.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scene with empty entities array should load successfully
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Empty(activeEntities);
    }

    [Fact]
    public void LoadGameScene_WithMultipleRules_LoadsEntitiesSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/multiple-rules.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify scene loaded (should have 1 entity: goblin)
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Single(activeEntities);
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out _));
    }

    [Fact]
    public void LoadGameScene_WithValidParent_MigratesParentBehavior()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/parent-valid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify entities loaded
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var player));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out var weapon));
        
        // test-architect: Verify parent-child relationship was established
        Assert.True(weapon.Value.TryGetParent(out var parent), "weapon should have parent");
        Assert.NotNull(parent);
        Assert.Equal(player.Value.Index, parent.Value.Index);
        
        // test-architect: Verify player has weapon as child
        var playerChildren = player.Value.GetChildren().ToList();
        Assert.Single(playerChildren);
        Assert.Contains(weapon.Value, playerChildren);
    }

    [Fact]
    public void LoadGameScene_WithInvalidParent_LoadsEntitiesWithoutParentRelationship()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/parent-invalid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Entities should still load
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out var weapon));
        
        // test-architect: weapon should NOT have parent since selector was invalid
        Assert.False(weapon.Value.TryGetParent(out _), "weapon should not have parent due to invalid selector");
    }

    [Fact]
    public void LoadGameScene_WithoutRulesField_LoadsSuccessfully()
    {
        // Arrange
        // test-architect: parent-valid.json doesn't have a rules field - should load fine
        var scenePath = "Scenes/Fixtures/QueryCommand/parent-valid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scene without rules field should load successfully
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out _));
    }

    [Fact]
    public void LoadGameScene_RulesDoNotExecute_OnlyParse()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/poison-rule.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Stage 1 is PARSER ONLY - rules should NOT execute
        // Verify goblin entity still has original health value (50, not modified by rule)
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblin));
        
        // test-architect: Verify properties behavior exists and health is unchanged
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin.Value, out var properties));
        Assert.NotNull(properties);
        
        // test-architect: Health should still be 50 (rule didn't execute)
        Assert.True(properties.Value.Properties.ContainsKey("health"), "health property should exist");
        Assert.True(properties.Value.Properties["health"].TryMatch(out float healthFloat), "health should be float");
        Assert.Equal(50f, healthFloat); // JSON numbers can deserialize as float
    }

    [Fact]
    public void ResetEvent_ClearsLoadedEntities()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/poison-rule.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // test-architect: Verify scene loaded
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out _));

        // Act
        EventBus<ResetEvent>.Push(new());

        // Assert
        // test-architect: After reset, entities should be cleared
        Assert.False(EntityIdRegistry.Instance.TryResolve("goblin", out _));
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Empty(activeEntities);
    }
}
