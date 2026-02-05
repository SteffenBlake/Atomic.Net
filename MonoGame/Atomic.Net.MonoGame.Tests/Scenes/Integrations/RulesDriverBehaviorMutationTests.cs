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

        // Assert: Error fired for invalid tag
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));

        // Assert: Valid tags applied (graceful degradation)
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var entity));
        Assert.True(BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity.Value, out var tags));
        Assert.Equal(2, tags.Value.Tags.Count); // validTag + anotherTag (12345 skipped)
        Assert.Contains("validTag", tags.Value.Tags);
        Assert.Contains("anotherTag", tags.Value.Tags);
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

    // ========== Transform Individual Field Tests ==========

    [Fact]
    public void RunFrame_WithTransformPositionXMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-position-x-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Position X updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(100f, transform.Value.Position.X);
    }

    [Fact]
    public void RunFrame_WithTransformPositionXMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-position-x-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformPositionYMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-position-y-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Position Y updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(200f, transform.Value.Position.Y);
    }

    [Fact]
    public void RunFrame_WithTransformPositionYMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-position-y-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformPositionZMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-position-z-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Position Z updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(50f, transform.Value.Position.Z);
    }

    [Fact]
    public void RunFrame_WithTransformPositionZMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-position-z-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformRotationXMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-x-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Rotation X updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(0.707f, transform.Value.Rotation.X, 0.001f);
    }

    [Fact]
    public void RunFrame_WithTransformRotationXMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-x-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformRotationYMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-y-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Rotation Y updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(0.707f, transform.Value.Rotation.Y, 0.001f);
    }

    [Fact]
    public void RunFrame_WithTransformRotationYMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-y-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformRotationZMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-z-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Rotation Z updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(0.5f, transform.Value.Rotation.Z, 0.001f);
    }

    [Fact]
    public void RunFrame_WithTransformRotationZMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-z-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformRotationWMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-w-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Rotation W updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("cube", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(0.866f, transform.Value.Rotation.W, 0.001f);
    }

    [Fact]
    public void RunFrame_WithTransformRotationWMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-rotation-w-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformScaleXMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-scale-x-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Scale X updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("sprite", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(2.0f, transform.Value.Scale.X);
    }

    [Fact]
    public void RunFrame_WithTransformScaleXMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-scale-x-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformScaleYMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-scale-y-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Scale Y updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("sprite", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(2.5f, transform.Value.Scale.Y);
    }

    [Fact]
    public void RunFrame_WithTransformScaleYMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-scale-y-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformScaleZMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-scale-z-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Scale Z updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("sprite", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(3.0f, transform.Value.Scale.Z);
    }

    [Fact]
    public void RunFrame_WithTransformScaleZMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-scale-z-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformAnchorXMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-anchor-x-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Anchor X updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("ui-element", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(0.5f, transform.Value.Anchor.X);
    }

    [Fact]
    public void RunFrame_WithTransformAnchorXMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-anchor-x-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformAnchorYMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-anchor-y-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Anchor Y updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("ui-element", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(0.5f, transform.Value.Anchor.Y);
    }

    [Fact]
    public void RunFrame_WithTransformAnchorYMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-anchor-y-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithTransformAnchorZMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-anchor-z-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: Anchor Z updated
        Assert.True(EntityIdRegistry.Instance.TryResolve("ui-element", out var entity));
        Assert.True(BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity.Value, out var transform));
        Assert.Equal(1.0f, transform.Value.Anchor.Z);
    }

    [Fact]
    public void RunFrame_WithTransformAnchorZMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/transform-anchor-z-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    // ========== Flex Two-Field Individual Tests ==========

    [Fact]
    public void RunFrame_WithFlexHeightValueMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-height-value-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexHeight value set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexHeightBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(100.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexHeightValueMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-height-value-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexHeightPercentMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-height-percent-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexHeight percent set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexHeightBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.True(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexHeightPercentMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-height-percent-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexWidthValueMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-width-value-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexWidth value set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexWidthBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(200.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexWidthValueMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-width-value-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexWidthPercentMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-width-percent-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexWidth percent set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexWidthBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.True(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexWidthPercentMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-width-percent-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionBottomValueMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-bottom-value-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPositionBottom value set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionBottomBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(10.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPositionBottomValueMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-bottom-value-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionBottomPercentMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-bottom-percent-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPositionBottom percent set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionBottomBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.True(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexPositionBottomPercentMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-bottom-percent-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionLeftValueMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-left-value-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPositionLeft value set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionLeftBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(50.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPositionLeftValueMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-left-value-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionLeftPercentMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-left-percent-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPositionLeft percent set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionLeftBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.False(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexPositionLeftPercentMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-left-percent-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionRightValueMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-right-value-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPositionRight value set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionRightBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(20.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPositionRightValueMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-right-value-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionRightPercentMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-right-percent-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPositionRight percent set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionRightBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.True(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexPositionRightPercentMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-right-percent-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionTopValueMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-top-value-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPositionTop value set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionTopBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(30.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPositionTopValueMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-top-value-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPositionTopPercentMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-top-percent-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPositionTop percent set
        Assert.True(EntityIdRegistry.Instance.TryResolve("item", out var entity));
        Assert.True(BehaviorRegistry<FlexPositionTopBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.False(behavior.Value.Percent);
    }

    [Fact]
    public void RunFrame_WithFlexPositionTopPercentMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-position-top-percent-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    // ========== Additional Flex Float Behavior Tests ==========

    [Fact]
    public void RunFrame_WithFlexBorderBottomMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-border-bottom-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexBorderBottom set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexBorderBottomBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(3.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexBorderBottomMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-border-bottom-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexBorderRightMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-border-right-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexBorderRight set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexBorderRightBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(4.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexBorderRightMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-border-right-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexBorderTopMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-border-top-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexBorderTop set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexBorderTopBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(2.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexBorderTopMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-border-top-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexMarginBottomMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-margin-bottom-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexMarginBottom set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexMarginBottomBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(8.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexMarginBottomMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-margin-bottom-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexMarginRightMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-margin-right-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexMarginRight set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexMarginRightBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(12.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexMarginRightMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-margin-right-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexMarginTopMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-margin-top-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexMarginTop set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexMarginTopBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(6.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexMarginTopMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-margin-top-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPaddingBottomMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-padding-bottom-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPaddingBottom set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexPaddingBottomBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(7.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPaddingBottomMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-padding-bottom-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPaddingRightMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-padding-right-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPaddingRight set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexPaddingRightBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(9.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPaddingRightMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-padding-right-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void RunFrame_WithFlexPaddingTopMutation_AppliesCorrectly()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-padding-top-mutation.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);

        // Assert: FlexPaddingTop set
        Assert.True(EntityIdRegistry.Instance.TryResolve("box", out var entity));
        Assert.True(BehaviorRegistry<FlexPaddingTopBehavior>.Instance.TryGetBehavior(entity.Value, out var behavior));
        Assert.Equal(4.0f, behavior.Value.Value);
    }

    [Fact]
    public void RunFrame_WithFlexPaddingTopMutation_Invalid_FiresError()
    {
        // Arrange
        SceneLoader.Instance.LoadGameScene("Scenes/Fixtures/RulesDriver/flex-padding-top-mutation-invalid.json");

        // Act
        RulesDriver.Instance.RunFrame(0.016f);

        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.False(string.IsNullOrEmpty(error.Message));
    }
}
