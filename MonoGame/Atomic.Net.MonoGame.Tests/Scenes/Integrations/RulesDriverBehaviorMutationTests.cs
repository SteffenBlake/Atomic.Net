using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Tags;
using Xunit;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

/// <summary>
/// Integration tests for RulesDriver behavior mutations.
/// Tests that all writable behaviors can be mutated via rules engine with positive and negative test cases.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class RulesDriverBehaviorMutationTests : IDisposable
{
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public RulesDriverBehaviorMutationTests()
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

    // ========== IdBehavior Tests ==========
    
    [Fact]
    public void RunFrame_WithIdBehaviorMutation_AppliesCorrectly()
    {
        // Arrange: Load fixture with entity "entity1"
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/id-mutation.json");
        
        // Act: Rule changes id from "entity1" to "renamedEntity"
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Entity now has new id
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity1", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("renamedEntity", out var entity));
        Assert.True(BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity.Value, out var idBehavior));
        Assert.Equal("renamedEntity", idBehavior.Value.Id);
    }
    
    [Fact]
    public void RunFrame_WithIdBehaviorMutation_Invalid_FiresError()
    {
        // Arrange: Load fixture attempting to set non-string id
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/id-mutation-invalid.json");
        
        // Act: Rule tries to set id to numeric value (invalid)
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.Contains("id", error.Message, StringComparison.OrdinalIgnoreCase);
        
        // Assert: Id unchanged
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity1", out _));
    }
    
    // ========== TagsBehavior Tests ==========
    
    [Fact]
    public void RunFrame_WithTagsBehaviorMutation_AppliesCorrectly()
    {
        // Arrange: Entity starts with tags ["enemy"]
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/tags-mutation.json");
        
        // Act: Rule adds "boss" tag
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Entity has both "enemy" and "boss"
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var entity));
        Assert.True(BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity.Value, out var tags));
        Assert.Contains("enemy", tags.Value.Tags);
        Assert.Contains("boss", tags.Value.Tags);
    }
    
    [Fact]
    public void RunFrame_WithTagsBehaviorMutation_Invalid_FiresError()
    {
        // Arrange: Fixture with malformed tags mutation (non-string tag)
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/tags-mutation-invalid.json");
        
        // Act: Rule tries to add numeric tag
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        
        // Assert: Tags unchanged
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var entity));
        Assert.True(BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity.Value, out var tags));
        Assert.Single(tags.Value.Tags); // Only original "enemy" tag
    }
}
