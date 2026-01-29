using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Flex;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Transform;
using FlexLayoutSharp;
using Microsoft.Xna.Framework;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

/// <summary>
/// Integration tests for RulesDriver behavior mutations.
/// Tests that all writable behaviors can be mutated via rules engine with positive and negative test cases.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class RulesDriverBehaviorMutationTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public RulesDriverBehaviorMutationTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        // Clean up between tests
        _errorListener.Dispose();
        _errorLogger.Dispose();

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
        Assert.False(string.IsNullOrEmpty(error.Message));
        
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
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
        
        // Assert: Tags unchanged
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var entity));
        Assert.True(BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity.Value, out var tags));
        Assert.Single(tags.Value.Tags); // Only original "enemy" tag
    }

    // ========== TransformBehavior Tests ==========

    [Fact]
    public void RunFrame_WithTransformPositionMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-position-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Position updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(100f, transform.Value.Position.X);
        Assert.Equal(200f, transform.Value.Position.Y);
        Assert.Equal(50f, transform.Value.Position.Z);
    }

    [Fact]
    public void RunFrame_WithTransformPositionMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-position-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformRotationMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Rotation updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(0.707f, transform.Value.Rotation.Y, 0.001f);
        Assert.Equal(0.707f, transform.Value.Rotation.W, 0.001f);
    }

    [Fact]
    public void RunFrame_WithTransformRotationMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformScaleMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-scale-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Scale updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("sprite", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(2f, transform.Value.Scale.X);
        Assert.Equal(2f, transform.Value.Scale.Y);
    }

    [Fact]
    public void RunFrame_WithTransformScaleMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-scale-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformAnchorMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-anchor-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Anchor updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("ui-element", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(0.5f, transform.Value.Anchor.X);
        Assert.Equal(0.5f, transform.Value.Anchor.Y);
    }

    [Fact]
    public void RunFrame_WithTransformAnchorMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-anchor-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    // ========== ParentBehavior Tests ==========

    [Fact]
    public void RunFrame_WithParentBehaviorMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/parent-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        Assert.Empty(_errorListener.ReceivedEvents);
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("child-node", out var entity));
        Assert.True(BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(entity.Value, out var parent));
        Assert.Equal("@parent-node", parent.Value.ParentSelector.ToString());
    }

    [Fact]
    public void RunFrame_WithParentBehaviorMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/parent-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    // ========== Flex Enum Behavior Tests ==========

    [Fact]
    public void RunFrame_WithFlexAlignItemsMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-align-items-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexAlignItems set
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var entity));
        Assert.True(BehaviorRegistry<FlexAlignItemsBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(Align.Center, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexAlignItemsMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-align-items-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexAlignSelfMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-align-self-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexAlignSelf set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexAlignSelfBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(Align.FlexStart, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexAlignSelfMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-align-self-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexDirectionMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-direction-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexDirection set
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var entity));
        Assert.True(BehaviorRegistry<FlexDirectionBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(FlexDirection.Row, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexDirectionMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-direction-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexWrapMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-wrap-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexWrap set
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var entity));
        Assert.True(BehaviorRegistry<FlexWrapBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(Wrap.Wrap, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexWrapMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-wrap-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexJustifyContentMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-justify-content-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexJustifyContent set
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var entity));
        Assert.True(BehaviorRegistry<FlexJustifyContentBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(Justify.SpaceBetween, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexJustifyContentMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-justify-content-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionTypeMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-type-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexPositionType set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionTypeBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(PositionType.Absolute, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPositionTypeMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-type-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    // ========== Flex Float Behavior Tests ==========

    [Fact]
    public void RunFrame_WithFlexBorderLeftMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-border-left-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexBorderLeft set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexBorderLeftBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(5.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexBorderLeftMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-border-left-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexGrowMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-grow-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexGrow set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexGrowBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(1.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexGrowMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-grow-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexMarginLeftMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-margin-left-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexMarginLeft set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexMarginLeftBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(10.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexMarginLeftMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-margin-left-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPaddingLeftMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-padding-left-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexPaddingLeft set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexPaddingLeftBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(5.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPaddingLeftMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-padding-left-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    // ========== Flex Two-Field Behavior Tests ==========

    [Fact]
    public void RunFrame_WithFlexHeightMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-height-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexHeight set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexHeightBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(100.0f, behavior.Value.Value);
        Assert.False(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexHeightMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-height-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexWidthMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-width-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexWidth set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexWidthBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(200.0f, behavior.Value.Value);
        Assert.True(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexWidthMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-width-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionLeftMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-left-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexPositionLeft set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionLeftBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(50.0f, behavior.Value.Value);
        Assert.False(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexPositionLeftMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-left-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    // ========== Flex Int Behavior Tests ==========

    [Fact]
    public void RunFrame_WithFlexZOverrideMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-zoverride-mutation.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: FlexZOverride set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexZOverride>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(10, behavior.Value.ZIndex);
    }

    [Fact]
    public void RunFrame_WithFlexZOverrideMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-zoverride-mutation-invalid.json");
        
        // Act
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }
}
