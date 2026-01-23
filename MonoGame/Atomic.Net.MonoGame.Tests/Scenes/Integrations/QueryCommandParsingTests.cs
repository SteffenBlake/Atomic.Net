using System.Text.Json;
using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
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

    [Fact(Skip = "EntitySelectorV2 has no JsonConverter - waiting for @senior-dev to implement")]
    public void LoadScene_WithPoisonRule_ParsesSimpleTagSelector()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/poison-rule.json";
        var jsonText = File.ReadAllText(scenePath);
        
        // Act
        // test-architect: This will throw JsonException - EntitySelectorV2 needs a JsonConverter
        var scene = JsonSerializer.Deserialize<JsonScene>(jsonText, JsonSerializerOptions.Web);
        
        // Assert
        Assert.NotNull(scene);
        Assert.NotNull(scene.Rules);
        Assert.Single(scene.Rules);
        
        var rule = scene.Rules[0];
        // test-architect: Once EntitySelectorV2 has a JsonConverter, verify:
        // rule.From should be TaggedEntitySelector variant with Tag = "poisoned"
        // rule.From.Matches(goblinEntity) should return true (when goblin has poisoned tag)
    }

    [Fact(Skip = "EntitySelectorV2 has no JsonConverter - waiting for @senior-dev to implement")]
    public void LoadScene_WithComplexPrecedence_ParsesUnionAndRefinement()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/complex-precedence.json";
        var jsonText = File.ReadAllText(scenePath);
        
        // Act
        var scene = JsonSerializer.Deserialize<JsonScene>(jsonText, JsonSerializerOptions.Web);
        
        // Assert
        Assert.NotNull(scene);
        Assert.NotNull(scene.Rules);
        Assert.Single(scene.Rules);
        
        var rule = scene.Rules[0];
        // test-architect: Once EntitySelectorV2 JsonConverter + TryParse are implemented, verify:
        // rule.From should be UnionEntitySelector with 2 children:
        //   - IdEntitySelector("player")
        //   - CollisionEnterEntitySelector(Next: TaggedEntitySelector("enemies", Next: TaggedEntitySelector("boss")))
        // Test precedence: ":" binds tighter than ","
    }

    [Fact(Skip = "EntitySelectorV2 has no JsonConverter - waiting for @senior-dev to implement")]
    public void LoadScene_WithInvalidSelector_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "Scenes/Tests/QueryCommand/invalid-selector.json";
        var jsonText = File.ReadAllText(scenePath);
        
        // Act
        var scene = JsonSerializer.Deserialize<JsonScene>(jsonText, JsonSerializerOptions.Web);
        
        // Assert
        // test-architect: Invalid selector "@@player" should fire ErrorEvent during JsonConverter.Read
        // Scene should parse but Rules field may be null or skip the invalid rule
        Assert.NotEmpty(errorListener.ReceivedEvents);
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("@@") || 
            e.Message.Contains("invalid") || 
            e.Message.Contains("selector"));
        Assert.NotEqual(default, errorEvent);
    }

    [Fact(Skip = "EntitySelectorV2 needs JsonConverter implementation - waiting for @senior-dev")]
    public void LoadScene_WithNoEntities_HasEmptyEntitiesList()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/no-entities.json";
        var jsonText = File.ReadAllText(scenePath);
        
        // Act
        var scene = JsonSerializer.Deserialize<JsonScene>(jsonText, JsonSerializerOptions.Web);
        
        // Assert
        Assert.NotNull(scene);
        Assert.NotNull(scene.Entities);
        Assert.Empty(scene.Entities);
        
        // test-architect: Rules field will fail to deserialize without EntitySelectorV2 JsonConverter
        // Once converter exists, verify: Assert.NotNull(scene.Rules); Assert.Single(scene.Rules);
    }

    [Fact(Skip = "EntitySelectorV2 has no JsonConverter - waiting for @senior-dev to implement")]
    public void LoadScene_WithMultipleRules_ParsesAllRules()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/multiple-rules.json";
        var jsonText = File.ReadAllText(scenePath);
        
        // Act
        var scene = JsonSerializer.Deserialize<JsonScene>(jsonText, JsonSerializerOptions.Web);
        
        // Assert
        Assert.NotNull(scene);
        Assert.NotNull(scene.Rules);
        Assert.Equal(2, scene.Rules.Count);
        
        // test-architect: Once EntitySelectorV2 JsonConverter is implemented, verify:
        // rules[0].From = TaggedEntitySelector("poisoned")
        // rules[1].From = TaggedEntitySelector("burning")
        // Both rules have valid Where and Do JsonNode data
    }

    [Fact]
    public void LoadScene_WithValidParent_EstablishesParentChildRelationship()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/parent-valid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Parent field uses old EntitySelector (not V2) via EntitySelectorConverter
        // This test verifies existing parent behavior still works
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out var playerEntity));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out var weaponEntity));
        
        Assert.True(weaponEntity.Value.TryGetParent(out var parent), "weapon should have parent");
        Assert.NotNull(parent);
        Assert.Equal(playerEntity.Value.Index, parent.Value.Index);
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
        // test-architect: Invalid parent "@@invalid" returns default EntitySelector from converter
        // TryLocate fails, fires ErrorEvent for unresolved reference
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out var weaponEntity));
        
        Assert.False(weaponEntity.Value.TryGetParent(out _), "weapon should not have parent - invalid selector");
        
        Assert.NotEmpty(errorListener.ReceivedEvents);
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e => 
            e.Message.Contains("invalid") || e.Message.Contains("Unresolved"));
        Assert.NotEqual(default, errorEvent);
    }

    [Fact]
    public void LoadScene_WithoutRulesField_LoadsEntitiesSuccessfully()
    {
        // Arrange
        var scenePath = "Scenes/Tests/QueryCommand/parent-valid.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Scenes without "rules" field are valid (Rules is nullable in JsonScene)
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        Assert.True(activeEntities.Count >= 2, "Scene without rules should load entities");
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("player", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("weapon", out _));
    }

    [Fact(Skip = "Scene validation not implemented - waiting for @senior-dev")]
    public void LoadScene_WithoutEntitiesField_FiresErrorEvent()
    {
        // test-architect: Per sprint requirements, ALL scenes must have "entities" array
        // Would need: missing-entities.json with no "entities" field
        // Then verify SceneLoader fires ErrorEvent during validation
    }

    [Fact(Skip = "Rules execution not in scope for Stage 1 - parser only")]
    public void LoadScene_VerifyRulesNotExecuted()
    {
        // test-architect: Stage 1 is parser only - rules should NOT be executed
        // Would verify entity state is unchanged after scene load
        // Example: goblin.health remains 50 (not reduced by poison rule)
    }
}
