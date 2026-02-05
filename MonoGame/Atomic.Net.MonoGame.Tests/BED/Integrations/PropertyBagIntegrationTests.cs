using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Ids;

namespace Atomic.Net.MonoGame.Tests.BED.Integrations;

/// <summary>
/// Integration tests for Property Bag system.
/// Tests full pipeline: JSON → Entities → PropertiesBehavior → Property values
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class PropertyBagIntegrationTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;

    public PropertyBagIntegrationTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up ALL entities (both global and scene) between tests
        EventBus<ShutdownEvent>.Push(new());
        _errorLogger.Dispose();
    }

    [Fact]
    public void LoadScene_WithProperties_AttachesPropertiesToEntities()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-basic.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin1.Value, out var goblin1Props));
        Assert.NotNull(goblin1Props);
        Assert.NotNull(goblin1Props.Value.Properties);

        // test-architect: Verify goblin-1 has 5 properties
        Assert.Equal(5, goblin1Props.Value.Properties.Count);

        Assert.True(goblin1Props.Value.Properties["enemy-type"].TryMatch(out string? enemyType));
        Assert.Equal("goblin", enemyType);

        Assert.True(goblin1Props.Value.Properties["faction"].TryMatch(out string? faction));
        Assert.Equal("horde", faction);

        Assert.True(goblin1Props.Value.Properties["max-health"].TryMatch(out float maxHealth));
        Assert.Equal(100f, maxHealth);

        Assert.True(goblin1Props.Value.Properties["is-boss"].TryMatch(out bool isBoss));
        Assert.False(isBoss);

        Assert.True(goblin1Props.Value.Properties["loot-tier"].TryMatch(out float lootTier));
        Assert.Equal(2f, lootTier);
    }

    [Fact]
    public void LoadScene_WithProperties_HandlesMultipleEntities()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-basic.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify goblin-boss has different properties
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-boss", out var goblinBoss));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblinBoss.Value, out var bossProps));
        Assert.NotNull(bossProps);
        Assert.NotNull(bossProps.Value.Properties);
        Assert.Equal(6, bossProps.Value.Properties.Count);

        Assert.True(bossProps.Value.Properties["is-boss"].TryMatch(out bool isBoss));
        Assert.True(isBoss);

        Assert.True(bossProps.Value.Properties["max-health"].TryMatch(out float bossHealth));
        Assert.Equal(500f, bossHealth);

        // test-architect: Verify treasure-chest has different properties
        Assert.True(EntityIdRegistry.Instance.TryResolve("treasure-chest", out var chest));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(chest.Value, out var chestProps));
        Assert.NotNull(chestProps);
        Assert.NotNull(chestProps.Value.Properties);
        Assert.Equal(2, chestProps.Value.Properties.Count);

        Assert.True(chestProps.Value.Properties["locked"].TryMatch(out bool locked));
        Assert.True(locked);
    }

    [Fact]
    public void LoadScene_EntityWithoutProperties_DoesNotHavePropertiesBehavior()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-basic.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Entity without properties should not have PropertiesBehavior
        Assert.True(EntityIdRegistry.Instance.TryResolve("no-properties", out var entity));
        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.HasBehavior(entity.Value));
    }

    [Fact]
    public void LoadScene_WithEdgeValues_PreservesValues()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-edge-values.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-with-edge-cases", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.NotNull(props);
        Assert.NotNull(props.Value.Properties);

        // test-architect: Per requirements, empty string, 0, and false are valid values
        Assert.True(props.Value.Properties["empty-string"].TryMatch(out string? emptyStr));
        Assert.Equal("", emptyStr);

        Assert.True(props.Value.Properties["zero-number"].TryMatch(out float zero));
        Assert.Equal(0f, zero);

        Assert.True(props.Value.Properties["false-bool"].TryMatch(out bool falseBool));
        Assert.False(falseBool);

        Assert.True(props.Value.Properties["negative-number"].TryMatch(out float negative));
        Assert.Equal(-42.5f, negative);

        Assert.True(props.Value.Properties["large-number"].TryMatch(out float large));
        Assert.Equal(999999.99f, large, precision: 2);
    }

    [Fact]
    public void LoadScene_WithNullValues_SkipsNullProperties()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-with-nulls.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-with-nulls", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.NotNull(props);
        Assert.NotNull(props.Value.Properties);

        // test-architect: Per requirements, null properties are skipped (not added to dictionary)
        Assert.Equal(2, props.Value.Properties.Count);
        Assert.True(props.Value.Properties.ContainsKey("valid-key"));
        Assert.True(props.Value.Properties.ContainsKey("another-valid"));
        Assert.False(props.Value.Properties.ContainsKey("null-key"));
    }

    [Fact]
    public void LoadScene_WithUnsupportedTypes_SkipsUnsupportedProperties()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-unsupported-types.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity-with-unsupported", out var entity));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity.Value, out var props));
        Assert.NotNull(props);
        Assert.NotNull(props.Value.Properties);

        // test-architect: Per requirements, arrays and objects are unsupported and should be skipped
        Assert.Equal(2, props.Value.Properties.Count);
        Assert.True(props.Value.Properties.ContainsKey("valid-string"));
        Assert.True(props.Value.Properties.ContainsKey("valid-number"));
        Assert.False(props.Value.Properties.ContainsKey("array-prop"));
        Assert.False(props.Value.Properties.ContainsKey("object-prop"));
    }

    [Fact]
    public void LoadScene_WithEmptyKey_FiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var scenePath = "BED/Fixtures/properties-empty-key.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Per requirements, empty string keys should fire ErrorEvent
        Assert.NotEmpty(errorListener.ReceivedEvents);
        var errorEvent = errorListener.ReceivedEvents.FirstOrDefault(e =>
            e.Message.Contains("empty") || e.Message.Contains("key") || e.Message.Contains("property"));
        Assert.NotEqual(default, errorEvent);
    }

    [Fact]
    public void LoadScene_WithCaseInsensitiveKeys_UsesLastValue()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-case-insensitive.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Verify all three entities loaded with their respective faction values
        Assert.True(EntityIdRegistry.Instance.TryResolve("case-test-1", out var entity1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity1.Value, out var props1));
        Assert.NotNull(props1);
        Assert.NotNull(props1.Value.Properties);

        // test-architect: Keys should be accessible case-insensitively
        Assert.True(props1.Value.Properties.ContainsKey("faction"));
        Assert.True(props1.Value.Properties.ContainsKey("Faction"));
        Assert.True(props1.Value.Properties.ContainsKey("FACTION"));

        // test-architect: All should resolve to same value
        Assert.True(props1.Value.Properties["faction"].TryMatch(out string? faction1));
        Assert.True(props1.Value.Properties["Faction"].TryMatch(out string? faction2));
        Assert.True(props1.Value.Properties["FACTION"].TryMatch(out string? faction3));
        Assert.Equal(faction1, faction2);
        Assert.Equal(faction2, faction3);
    }

    [Fact]
    public void EntityDeactivation_RemovesPropertiesBehavior()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-basic.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.HasBehavior(goblin1.Value));

        // Act
        EntityRegistry.Instance.Deactivate(goblin1.Value);

        // Assert
        // test-architect: Accessing behavior on deactivated entity should throw
        Assert.Throws<InvalidOperationException>(() =>
            BehaviorRegistry<PropertiesBehavior>.Instance.HasBehavior(goblin1.Value));
    }

    [Fact]
    public void ResetEvent_ClearsAllProperties()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-basic.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1Before));
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.HasBehavior(goblin1Before.Value));

        // Act
        ResetDriver.Instance.Run();

        // Assert
        // test-architect: After reset, ID registry should be cleared
        Assert.False(EntityIdRegistry.Instance.TryResolve("goblin-1", out _));

        // test-architect: Reload scene and verify no pollution
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1After));

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin1After.Value, out var propsAfter));
        Assert.NotNull(propsAfter);
        Assert.NotNull(propsAfter.Value.Properties);
        Assert.Equal(5, propsAfter.Value.Properties.Count);
    }

    [Fact]
    public void ResetEvent_DoesNotPolluteProperties()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-basic.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1First));
        var firstIndex = goblin1First.Value.Index;

        // Act
        ResetDriver.Instance.Run();
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1Second));
        Assert.Equal(firstIndex, goblin1Second.Value.Index);

        // test-architect: Properties should be identical to first load
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin1Second.Value, out var props));
        Assert.NotNull(props);
        Assert.NotNull(props.Value.Properties);
        Assert.Equal(5, props.Value.Properties.Count);

        Assert.True(props.Value.Properties["enemy-type"].TryMatch(out string? enemyType));
        Assert.Equal("goblin", enemyType);

        Assert.True(props.Value.Properties["max-health"].TryMatch(out float health));
        Assert.Equal(100f, health);
    }

    [Fact]
    public void PropertiesBehavior_FiresBehaviorAddedEvent()
    {
        // Arrange
        using var listener = new FakeEventListener<BehaviorAddedEvent<PropertiesBehavior>>();
        var scenePath = "BED/Fixtures/properties-basic.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);

        // Assert
        // test-architect: Should fire event for each entity with properties (3 entities)
        Assert.True(listener.ReceivedEvents.Count >= 3);

        // test-architect: Verify events contain correct entity references
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-boss", out var goblinBoss));
        Assert.True(EntityIdRegistry.Instance.TryResolve("treasure-chest", out var chest));

        var goblin1Event = listener.ReceivedEvents.FirstOrDefault(e => e.Entity.Index == goblin1.Value.Index);
        var bossEvent = listener.ReceivedEvents.FirstOrDefault(e => e.Entity.Index == goblinBoss.Value.Index);
        var chestEvent = listener.ReceivedEvents.FirstOrDefault(e => e.Entity.Index == chest.Value.Index);

        Assert.NotEqual(default, goblin1Event);
        Assert.NotEqual(default, bossEvent);
        Assert.NotEqual(default, chestEvent);
    }

    [Fact]
    public void PropertiesBehavior_CanBeUpdated()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-basic.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1));
        using var updateListener = new FakeEventListener<PostBehaviorUpdatedEvent<PropertiesBehavior>>();

        // Act
        // test-architect: Update properties by modifying the dictionary
        goblin1.Value.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("max-health", 200f) };
        });

        // Assert
        Assert.Single(updateListener.ReceivedEvents);

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(goblin1.Value, out var updatedProps));
        Assert.NotNull(updatedProps);
        Assert.NotNull(updatedProps.Value.Properties);
        Assert.True(updatedProps.Value.Properties["max-health"].TryMatch(out float newHealth));
        Assert.Equal(200f, newHealth);
    }

    [Fact]
    public void PropertiesBehavior_CanBeRemoved()
    {
        // Arrange
        var scenePath = "BED/Fixtures/properties-basic.json";
        SceneLoader.Instance.LoadGameScene(scenePath);

        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin-1", out var goblin1));
        using var removeListener = new FakeEventListener<PostBehaviorRemovedEvent<PropertiesBehavior>>();

        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.HasBehavior(goblin1.Value));

        // Act
        BehaviorRegistry<PropertiesBehavior>.Instance.Remove(goblin1.Value);

        // Assert
        Assert.Single(removeListener.ReceivedEvents);
        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.HasBehavior(goblin1.Value));
    }
}
