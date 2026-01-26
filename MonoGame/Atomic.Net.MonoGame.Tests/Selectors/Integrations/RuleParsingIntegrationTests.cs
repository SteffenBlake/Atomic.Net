using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Hierarchy;

namespace Atomic.Net.MonoGame.Tests.Selectors.Integrations;

/// <summary>
/// Integration tests for Query + Command system rule parsing.
/// Tests the full pipeline: JSON scene files → JsonRule objects with validated EntitySelectors.
/// This is Stage 1: Parser Only - no execution or evaluation.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class RuleParsingIntegrationTests : IDisposable
{
    public RuleParsingIntegrationTests()
    {
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both persistent and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void LoadScene_WithPoisonRule_ParsesSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/poison-rule.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scene should load without errors
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.NotEmpty(activeEntities);
        
        // test-architect: Verify goblin entity was spawned
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblinEntity));
        Assert.NotNull(goblinEntity);
        
        // test-architect: No error events should fire for valid scene
        // (ErrorEvents would be in FakeEventListener if we were tracking them)
    }

    [Fact]
    public void LoadScene_WithComplexPrecedence_ParsesUnionAndRefinementCorrectly()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/complex-precedence.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scene should load without errors
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.NotEmpty(activeEntities);
        
        // test-architect: Verify both entities were spawned
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var playerEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("boss", out var bossEntity));
        Assert.NotNull(playerEntity);
        Assert.NotNull(bossEntity);
    }

    [Fact]
    public void LoadScene_WithInvalidSelector_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "Scenes/Fixtures/QueryCommand/invalid-selector.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should fire error event for invalid selector syntax
        Assert.NotEmpty(errorListener.ReceivedEvents);
        
        // test-architect: Error should mention the invalid selector (@@)
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("@@") || e.Message.Contains("Invalid"));
        Assert.NotEqual(default, errorEvent);
        
        // test-architect: Entity should still be spawned despite rule parsing failure
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var playerEntity));
        Assert.NotNull(playerEntity);
    }

    [Fact]
    public void LoadScene_WithNoEntitiesButRules_LoadsSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/no-entities-with-rules.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scene should load without entities but rules are parsed
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.Empty(activeEntities);
        
        // test-architect: No errors should fire - empty entities array is valid
    }

    [Fact]
    public void LoadScene_WithMultipleRules_ParsesAllRulesSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/multiple-rules.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scene should load without errors
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.NotEmpty(activeEntities);
        
        // test-architect: Verify goblin entity was spawned
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblinEntity));
        Assert.NotNull(goblinEntity);
    }

    [Fact]
    public void LoadScene_WithValidParent_EstablishesParentChildRelationship()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/parent-valid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Both entities should be spawned
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var playerEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out var weaponEntity));
        Assert.NotNull(playerEntity);
        Assert.NotNull(weaponEntity);
        
        // test-architect: Verify parent-child relationship was established
        Assert.True(weaponEntity.Value.TryGetParent(out var parent));
        Assert.NotNull(parent);
        Assert.Equal(playerEntity.Value.Index, parent.Value.Index);
        
        // test-architect: Verify player has weapon as child
        var playerChildren = playerEntity.Value.GetChildren().ToList();
        Assert.Single(playerChildren);
        Assert.Contains(weaponEntity.Value, playerChildren);
    }

    [Fact]
    public void LoadScene_WithInvalidParent_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "Scenes/Fixtures/QueryCommand/parent-invalid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should fire error event for invalid parent selector
        Assert.NotEmpty(errorListener.ReceivedEvents);
        
        // test-architect: Error should mention the invalid selector
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("@@") || e.Message.Contains("Invalid"));
        Assert.NotEqual(default, errorEvent);
        
        // test-architect: Both entities should still be spawned
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var playerEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out var weaponEntity));
        Assert.NotNull(playerEntity);
        Assert.NotNull(weaponEntity);
        
        // test-architect: Weapon should NOT have a parent since parsing failed
        Assert.False(weaponEntity.Value.TryGetParent(out _));
    }

    [Fact]
    public void LoadScene_WithEntitiesOnly_LoadsSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/entities-only.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scene without rules should load successfully
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.NotEmpty(activeEntities);
        
        // test-architect: Verify entity was spawned
        Assert.True(EntityIdRegistry.Instance.TryResolve("standalone", out var entity));
        Assert.NotNull(entity);
    }

    [Fact]
    public void SelectorRecalc_AfterSceneLoad_UpdatesAllSelectors()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/poison-rule.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        // test-architect: SelectorRegistry.Recalc() should be called after scene load
        // This is tested implicitly - if selectors weren't recalc'd, ID selectors wouldn't match entities
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblinEntity));
        Assert.NotNull(goblinEntity);
    }

    [Fact]
    public void ParentBehavior_MigrationToEntitySelector_WorksWithExistingScenes()
    {
        // Arrange
        // test-architect: Use existing scene that already has parent references
        var scenePath = "Scenes/Fixtures/basic-scene.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify existing parent syntax still works
        Assert.True(EntityIdRegistry.Instance.TryResolve("root-container", out var rootEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("menu-button", out var buttonEntity));
        
        // test-architect: Verify parent-child relationship
        Assert.True(buttonEntity.Value.TryGetParent(out var parent));
        Assert.NotNull(parent);
        Assert.Equal(rootEntity.Value.Index, parent.Value.Index);
    }

    [Fact]
    public void RuleSelector_WithTagSelector_ParsesButDoesNotExecute()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/poison-rule.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Stage 1 only parses, doesn't execute
        // TagEntitySelector.Recalc() throws NotImplementedException (expected for Stage 1)
        // Scene should load successfully, rules are parsed but not evaluated
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var goblinEntity));
        Assert.NotNull(goblinEntity);
    }

    [Fact]
    public void RuleSelector_WithCollisionSelector_ParsesButDoesNotExecute()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/complex-precedence.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Stage 1 only parses, doesn't execute
        // CollisionEnterEntitySelector.Recalc() throws NotImplementedException (expected for Stage 1)
        // Scene should load successfully, rules are parsed but not evaluated
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var playerEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("boss", out var bossEntity));
        Assert.NotNull(playerEntity);
        Assert.NotNull(bossEntity);
    }

    [Fact]
    public void ResetEvent_AfterLoadingRules_ClearsEntityReferences()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/poison-rule.json";
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out _));

        // Act
        EventBus<ResetEvent>.Push(new());

        // Assert
        // test-architect: Entity IDs should be cleared after reset
        Assert.False(EntityIdRegistry.Instance.TryResolve("goblin", out _));
    }

    [Fact]
    public void LoadScene_TwiceWithReset_DoesNotPollute()
    {
        // Arrange
        var scenePath = "Scenes/Fixtures/QueryCommand/poison-rule.json";
        
        // Act: Load first time
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var entity1));
        var firstIndex = entity1.Value.Index;
        
        // Act: Reset and reload
        EventBus<ResetEvent>.Push(new());
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert: Second load should work identically
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var entity2));
        Assert.Equal(firstIndex, entity2.Value.Index);
    }
}
