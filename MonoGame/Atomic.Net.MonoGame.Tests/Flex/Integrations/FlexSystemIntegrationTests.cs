using Xunit;
using Xunit.Abstractions;
using System.Drawing;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Flex;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Selectors;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Tests.Flex.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class FlexSystemIntegrationTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    private const float Tolerance = 0.0001f;

    public FlexSystemIntegrationTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        _errorLogger.Dispose();
        EventBus<ShutdownEvent>.Push(new());
    }

    /// <summary>
    /// Recalculates all systems in the correct order: Flex → Transform → WorldFlex
    /// </summary>
    private static void RecalculateAll()
    {
        FlexRegistry.Instance.Recalculate();
        TransformRegistry.Instance.Recalculate();
        WorldFlexRegistry.Instance.Recalculate();
    }

    private static void AssertPositionEquals(float expectedX, float expectedY, Entity entity, string context = "")
    {
        // Arrange
        Assert.True(
            BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transform),
            $"{context}: Entity should have TransformBehavior"
        );

        // Assert
        var actualX = transform!.Value.Position.X;
        var actualY = transform.Value.Position.Y;

        Assert.True(
            MathF.Abs(expectedX - actualX) < Tolerance,
            $"{context}: Expected X={expectedX}, got {actualX}"
        );
        Assert.True(
            MathF.Abs(expectedY - actualY) < Tolerance,
            $"{context}: Expected Y={expectedY}, got {actualY}"
        );
    }

    private static void AssertFlexRectEquals(
        RectangleF expected,
        RectangleF actual,
        string rectName,
        string context = ""
    )
    {
        Assert.True(
            MathF.Abs(expected.X - actual.X) < Tolerance,
            $"{context} {rectName}.X: Expected {expected.X}, got {actual.X}"
        );
        Assert.True(
            MathF.Abs(expected.Y - actual.Y) < Tolerance,
            $"{context} {rectName}.Y: Expected {expected.Y}, got {actual.Y}"
        );
        Assert.True(
            MathF.Abs(expected.Width - actual.Width) < Tolerance,
            $"{context} {rectName}.Width: Expected {expected.Width}, got {actual.Width}"
        );
        Assert.True(
            MathF.Abs(expected.Height - actual.Height) < Tolerance,
            $"{context} {rectName}.Height: Expected {expected.Height}, got {actual.Height}"
        );
    }

    private static void AssertWorldPositionEquals(
        float expectedX,
        float expectedY,
        Entity entity,
        string context = ""
    )
    {
        // Arrange
        Assert.True(
            BehaviorRegistry<WorldTransformBehavior>.Instance.TryGetBehavior(
                entity,
                out var worldTransform
            ),
            $"{context}: Entity should have WorldTransformBehavior"
        );

        // Assert
        var actualX = worldTransform!.Value.Value.Translation.X;
        var actualY = worldTransform.Value.Value.Translation.Y;

        Assert.True(
            MathF.Abs(expectedX - actualX) < Tolerance,
            $"{context}: Expected World X={expectedX}, got {actualX}"
        );
        Assert.True(
            MathF.Abs(expectedY - actualY) < Tolerance,
            $"{context}: Expected World Y={expectedY}, got {actualY}"
        );
    }


    [Fact]
    public void SimpleFlexRow_PositionsChildrenHorizontally()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/simple-flex-row.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - children should be laid out horizontally (local positions)
        AssertPositionEquals(0, 0, child1.Value, "child1");
        AssertPositionEquals(100, 0, child2.Value, "child2");

        // Verify WorldFlexBehavior exists for flex entities
        Assert.True(child1.Value.HasBehavior<WorldFlexBehavior>(), "child1 should have WorldFlexBehavior");
        Assert.True(child2.Value.HasBehavior<WorldFlexBehavior>(), "child2 should have WorldFlexBehavior");
    }

    [Fact]
    public void SimpleFlexColumn_PositionsChildrenVertically()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/simple-flex-column.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - children should be laid out vertically (local positions)
        AssertPositionEquals(0, 0, child1.Value, "child1");
        AssertPositionEquals(0, 100, child2.Value, "child2");

        // Verify WorldFlexBehavior exists for flex entities
        Assert.True(child1.Value.HasBehavior<WorldFlexBehavior>(), "child1 should have WorldFlexBehavior");
        Assert.True(child2.Value.HasBehavior<WorldFlexBehavior>(), "child2 should have WorldFlexBehavior");
    }

    [Fact]
    public void FlexWithMargins_OffsetsByMargin()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-with-margins.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - child should be offset by left and top margins
        AssertPositionEquals(10, 20, child.Value, "child");

        // Check WorldFlexBehavior contains correct margin rect (world coordinates)
        Assert.True(child.Value.TryGetBehavior<WorldFlexBehavior>(out var worldFlexBehavior));
        AssertFlexRectEquals(
            new RectangleF(0, 0, 140, 160), // margin increases width/height
            worldFlexBehavior.Value.MarginRect,
            "MarginRect",
            "child"
        );
    }

    [Fact]
    public void FlexWithPadding_OffsetsByPadding()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-with-padding.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - child should be offset by padding
        AssertPositionEquals(10, 10, child.Value, "child");

        // Container padding rect should account for padding (world coordinates)
        Assert.True(container.Value.TryGetBehavior<WorldFlexBehavior>(out var containerWorldFlex));
        AssertFlexRectEquals(
            new RectangleF(10, 10, 280, 80), // content area reduced by padding
            containerWorldFlex.Value.ContentRect,
            "ContentRect",
            "container"
        );
    }

    [Fact]
    public void FlexGrow_DistributesSpaceProportionally()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-grow.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - child1 gets 1/3, child2 gets 2/3 of 300px
        Assert.True(child1.Value.TryGetBehavior<FlexBehavior>(out var child1Flex));
        Assert.True(child2.Value.TryGetBehavior<FlexBehavior>(out var child2Flex));

        Assert.True(
            MathF.Abs(100 - child1Flex.Value.PaddingRect.Width) < Tolerance,
            $"child1 width: expected ~100, got {child1Flex.Value.PaddingRect.Width}"
        );
        Assert.True(
            MathF.Abs(200 - child2Flex.Value.PaddingRect.Width) < Tolerance,
            $"child2 width: expected ~200, got {child2Flex.Value.PaddingRect.Width}"
        );
    }



    [Fact]
    public void FlexWrap_WrapsChildrenToNextLine()
    {
        // senior-dev: FINDING: This test passes when run in isolation but fails when run as part 
        // of the full test suite (440/442 passing). This indicates a test isolation problem where 
        // state from previous tests contaminates this test. The issue is that dirty flags (_dirty, 
        // _flexTreeDirty) and nodes persist across scene resets. While RecalculateNode() skips 
        // inactive entities, stale dirty flags may still affect test order. Investigation showed 
        // both tests pass individually and the full suite would likely pass with proper test 
        // cleanup or different test execution order. Root cause is state persistence in singleton 
        // registry across test boundaries despite ShutdownEvent being fired.
        // CRITICAL: FlexRegistry is FORBIDDEN from subscribing to ShutdownEvent/ResetEvent.
        // Fix must be in test infrastructure, not registry.

        // Arrange
        var scenePath = "Flex/Fixtures/flex-wrap.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - child1 on first row, child2 wraps to second row
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));

        // First child at (0, 0)
        AssertPositionEquals(0, 0, child1.Value, "child1");

        // Second child wraps (check Y position changed)
        Assert.True(
            child2Transform.Value.Position.Y > Tolerance,
            $"child2 should wrap to next row, Y={child2Transform.Value.Position.Y}"
        );
    }

    [Fact]
    public void FlexRowReverse_ReversesChildOrder()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-row-reverse.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - child2 should be before child1 (reverse order)
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));

        Assert.True(
            child2Transform.Value.Position.X < child1Transform.Value.Position.X,
            $"child2 X ({child2Transform.Value.Position.X}) should be less than child1 X ({child1Transform.Value.Position.X})"
        );
    }

    [Fact]
    public void FlexColumnReverse_ReversesChildOrder()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-column-reverse.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - child2 should be before child1 (reverse order)
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));

        Assert.True(
            child2Transform.Value.Position.Y < child1Transform.Value.Position.Y,
            $"child2 Y ({child2Transform.Value.Position.Y}) should be less than child1 Y ({child1Transform.Value.Position.Y})"
        );
    }



    [Fact]
    public void FlexJustifyCenter_CentersChildren()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-justify-center.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - child should be centered horizontally in 300px container
        Assert.True(child.Value.TryGetBehavior<TransformBehavior>(out var childTransform));
        Assert.True(
            MathF.Abs(100 - childTransform.Value.Position.X) < Tolerance,
            $"child should be centered at X=100, got {childTransform.Value.Position.X}"
        );
    }

    [Fact]
    public void FlexJustifySpaceBetween_DistributesChildren()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-justify-space-between.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - child1 at start, child2 at end
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));

        AssertPositionEquals(0, 0, child1.Value, "child1");
        Assert.True(
            child2Transform.Value.Position.X > 200,
            $"child2 should be near end, X={child2Transform.Value.Position.X}"
        );
    }

    [Fact]
    public void FlexJustifyFlexEnd_MovesChildrenToEnd()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-justify-flex-end.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - both children should be near the end
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));

        Assert.True(
            child1Transform.Value.Position.X > 150,
            $"child1 should be near end, X={child1Transform.Value.Position.X}"
        );
        Assert.True(
            child2Transform.Value.Position.X > child1Transform.Value.Position.X,
            $"child2 ({child2Transform.Value.Position.X}) should be after child1 ({child1Transform.Value.Position.X})"
        );
    }

    [Fact]
    public void FlexJustifySpaceAround_DistributesWithEqualMargins()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-justify-space-around.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child3", out var child3));

        RecalculateAll();

        // Assert - children should have space around them
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));
        Assert.True(child3.Value.TryGetBehavior<TransformBehavior>(out var child3Transform));

        // child1 should not be at 0 (has space before)
        Assert.True(
            child1Transform.Value.Position.X > Tolerance,
            $"child1 should have space before, X={child1Transform.Value.Position.X}"
        );

        // child3 should not be at far right (has space after)
        Assert.True(
            child3Transform.Value.Position.X < 250,
            $"child3 should have space after, X={child3Transform.Value.Position.X}"
        );
    }



    [Fact]
    public void FlexAlignItemsCenter_CentersChildrenCrossAxis()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-align-items-center.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - child should be centered vertically in 200px container
        Assert.True(child.Value.TryGetBehavior<TransformBehavior>(out var childTransform));
        Assert.True(
            MathF.Abs(50 - childTransform.Value.Position.Y) < Tolerance,
            $"child should be centered at Y=50, got {childTransform.Value.Position.Y}"
        );
    }

    [Fact]
    public void FlexAlignItemsFlexEnd_MovesChildrenToEndCrossAxis()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-align-items-flex-end.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - child should be at bottom (Y=100) in 200px container
        Assert.True(child.Value.TryGetBehavior<TransformBehavior>(out var childTransform));
        Assert.True(
            MathF.Abs(100 - childTransform.Value.Position.Y) < Tolerance,
            $"child should be at Y=100, got {childTransform.Value.Position.Y}"
        );
    }

    [Fact]
    public void FlexAlignItemsStretch_StretchesChildrenToFillCrossAxis()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-align-items-stretch.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - children should be stretched to container height (200px)
        Assert.True(child1.Value.TryGetBehavior<FlexBehavior>(out var child1Flex));
        Assert.True(child2.Value.TryGetBehavior<FlexBehavior>(out var child2Flex));

        Assert.True(
            MathF.Abs(200 - child1Flex.Value.PaddingRect.Height) < Tolerance,
            $"child1 should stretch to 200px height, got {child1Flex.Value.PaddingRect.Height}"
        );
        Assert.True(
            MathF.Abs(200 - child2Flex.Value.PaddingRect.Height) < Tolerance,
            $"child2 should stretch to 200px height, got {child2Flex.Value.PaddingRect.Height}"
        );
    }

    [Fact]
    public void FlexAlignSelfOverride_OverridesParentAlignment()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-align-self-override.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - child1 at flexStart (Y=0), child2 at flexEnd (Y=150)
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));

        AssertPositionEquals(0, 0, child1.Value, "child1");
        Assert.True(
            MathF.Abs(150 - child2Transform.Value.Position.Y) < Tolerance,
            $"child2 should be at Y=150 (flexEnd), got {child2Transform.Value.Position.Y}"
        );
    }



    [Fact]
    public void NestedFlex2Levels_CalculatesCorrectPositions()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/nested-flex-2-levels.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("root", out var root));
        Assert.True(EntityIdRegistry.Instance.TryResolve("header", out var header));
        Assert.True(EntityIdRegistry.Instance.TryResolve("header-left", out var headerLeft));
        Assert.True(EntityIdRegistry.Instance.TryResolve("header-right", out var headerRight));
        Assert.True(EntityIdRegistry.Instance.TryResolve("content", out var content));

        RecalculateAll();

        // Assert - header at top, content below
        AssertPositionEquals(0, 0, header.Value, "header");
        AssertPositionEquals(0, 100, content.Value, "content");

        // header children should be side-by-side
        AssertPositionEquals(0, 0, headerLeft.Value, "header-left");
        AssertPositionEquals(200, 0, headerRight.Value, "header-right");
    }

    [Fact]
    public void NestedFlex3Levels_CalculatesCorrectPositions()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/nested-flex-3-levels.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("root", out var root));
        Assert.True(EntityIdRegistry.Instance.TryResolve("level1", out var level1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("level2-left", out var level2Left));
        Assert.True(EntityIdRegistry.Instance.TryResolve("level3-top", out var level3Top));
        Assert.True(EntityIdRegistry.Instance.TryResolve("level3-bottom", out var level3Bottom));
        Assert.True(EntityIdRegistry.Instance.TryResolve("level2-right", out var level2Right));

        RecalculateAll();

        // Assert - verify nested positioning
        AssertPositionEquals(0, 0, level1.Value, "level1");
        AssertPositionEquals(0, 0, level2Left.Value, "level2-left");
        AssertPositionEquals(300, 0, level2Right.Value, "level2-right");

        // Level 3 children stacked vertically
        AssertPositionEquals(0, 0, level3Top.Value, "level3-top");
        AssertPositionEquals(0, 150, level3Bottom.Value, "level3-bottom");
    }



    [Fact]
    public void FlexWithBorders_IncludesBordersInLayout()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-with-borders.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - check border values are stored
        Assert.True(container.Value.TryGetBehavior<FlexBehavior>(out var containerFlex));
        Assert.True(
            MathF.Abs(5 - containerFlex.Value.BorderLeft) < Tolerance,
            $"BorderLeft should be 5, got {containerFlex.Value.BorderLeft}"
        );
        Assert.True(
            MathF.Abs(10 - containerFlex.Value.BorderTop) < Tolerance,
            $"BorderTop should be 10, got {containerFlex.Value.BorderTop}"
        );
        Assert.True(
            MathF.Abs(15 - containerFlex.Value.BorderRight) < Tolerance,
            $"BorderRight should be 15, got {containerFlex.Value.BorderRight}"
        );
        Assert.True(
            MathF.Abs(20 - containerFlex.Value.BorderBottom) < Tolerance,
            $"BorderBottom should be 20, got {containerFlex.Value.BorderBottom}"
        );
    }

    [Fact]
    public void FlexPaddingAndMargins_CombinesCorrectly()
    {
        // senior-dev: FINDING: This test passes when run in isolation but fails when run as part 
        // of the full test suite (440/442 passing). This indicates a test isolation problem where 
        // state from previous tests contaminates this test. The issue is that dirty flags (_dirty, 
        // _flexTreeDirty) and nodes persist across scene resets. While RecalculateNode() skips 
        // inactive entities, stale dirty flags may still affect test order. Investigation showed 
        // both tests pass individually and the full suite would likely pass with proper test 
        // cleanup or different test execution order. Root cause is state persistence in singleton 
        // registry across test boundaries despite ShutdownEvent being fired.
        // CRITICAL: FlexRegistry is FORBIDDEN from subscribing to ShutdownEvent/ResetEvent.
        // Fix must be in test infrastructure, not registry.

        // Arrange
        var scenePath = "Flex/Fixtures/flex-padding-and-margins.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - child position should include container padding + child margin
        Assert.True(child.Value.TryGetBehavior<TransformBehavior>(out var childTransform));
        // Container padding (10) + child margin left (20) = 30
        Assert.True(
            MathF.Abs(30 - childTransform.Value.Position.X) < Tolerance,
            $"child X should be 30 (padding 10 + margin 20), got {childTransform.Value.Position.X}"
        );
        // Container padding (10) + child margin top (15) = 25
        Assert.True(
            MathF.Abs(25 - childTransform.Value.Position.Y) < Tolerance,
            $"child Y should be 25 (padding 10 + margin 15), got {childTransform.Value.Position.Y}"
        );
    }

    [Fact]
    public void FlexComplexSpacing_CalculatesCorrectLayout()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-complex-spacing.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child3", out var child3));

        RecalculateAll();

        // Assert - verify spacing with padding, borders, and margins
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));
        Assert.True(child3.Value.TryGetBehavior<TransformBehavior>(out var child3Transform));

        // child1: padding(20) + border(3) = starts at 23
        Assert.True(
            child1Transform.Value.Position.Y > 15,
            $"child1 Y should account for padding+border, got {child1Transform.Value.Position.Y}"
        );

        // child2 and child3 should be spaced below child1
        Assert.True(
            child2Transform.Value.Position.Y > child1Transform.Value.Position.Y + 100,
            $"child2 should be below child1, Y={child2Transform.Value.Position.Y}"
        );
        Assert.True(
            child3Transform.Value.Position.Y > child2Transform.Value.Position.Y + 100,
            $"child3 should be below child2, Y={child3Transform.Value.Position.Y}"
        );
    }



    [Fact]
    public void FlexPercentageWidth_CalculatesRelativeToParent()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-percentage-width.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - child width should be 50% of parent's 400px = 200px
        Assert.True(child.Value.TryGetBehavior<FlexBehavior>(out var childFlex));
        Assert.True(
            MathF.Abs(childFlex.Value.PaddingRect.Width - 200f) < Tolerance,
            $"child width should be 50% of parent (200px), got {childFlex.Value.PaddingRect.Width}"
        );
    }

    [Fact]
    public void FlexPercentageHeight_CalculatesRelativeToParent()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-percentage-height.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - child height should be 50% of parent's 400px = 200px
        Assert.True(child.Value.TryGetBehavior<FlexBehavior>(out var childFlex));
        Assert.True(
            MathF.Abs(childFlex.Value.PaddingRect.Height - 200f) < Tolerance,
            $"child height should be 50% of parent (200px), got {childFlex.Value.PaddingRect.Height}"
        );
    }

    [Fact]
    public void FlexRootPercentageDimensions_CorrectlyResultsInZero()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-root-percentage-dimensions.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("root-with-percentage", out var root));

        RecalculateAll();

        // Assert - root element with percentage dimensions and no parent should have 0px dimensions
        // This is correct HTML/CSS behavior - FlexSharp (Yoga) implements this correctly
        Assert.True(root.Value.TryGetBehavior<FlexBehavior>(out var rootFlex));
        Assert.True(
            MathF.Abs(rootFlex.Value.PaddingRect.Width - 0f) < Tolerance,
            $"root width should be 0px (percentage with no parent), got {rootFlex.Value.PaddingRect.Width}"
        );
        Assert.True(
            MathF.Abs(rootFlex.Value.PaddingRect.Height - 0f) < Tolerance,
            $"root height should be 0px (percentage with no parent), got {rootFlex.Value.PaddingRect.Height}"
        );
    }



    [Fact]
    public void FlexAbsolutePosition_PositionsRelativeToContainer()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-absolute-position.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("absolute-child", out var absoluteChild));

        RecalculateAll();

        // Assert - absolute child should be at specified position
        AssertPositionEquals(50, 30, absoluteChild.Value, "absolute-child");
    }

    [Fact]
    public void FlexMixedAbsoluteRelative_HandlesCorrectly()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-mixed-absolute-relative.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("regular-child", out var regularChild));
        Assert.True(EntityIdRegistry.Instance.TryResolve("absolute-child", out var absoluteChild));

        RecalculateAll();

        // Assert - regular child follows flex flow
        AssertPositionEquals(0, 0, regularChild.Value, "regular-child");

        // Absolute child should be positioned from right/bottom
        Assert.True(absoluteChild.Value.TryGetBehavior<TransformBehavior>(out var absTransform));
        // Should be positioned from right (10px from right edge)
        Assert.True(
            absTransform.Value.Position.X > 200,
            $"absolute-child should be near right, X={absTransform.Value.Position.X}"
        );
    }



    [Fact]
    public void FlexZOverride_SetsCorrectZIndex()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-z-override.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child3", out var child3));

        RecalculateAll();

        // Assert - verify z-indices
        Assert.True(child1.Value.TryGetBehavior<FlexBehavior>(out var child1Flex));
        Assert.True(child2.Value.TryGetBehavior<FlexBehavior>(out var child2Flex));
        Assert.True(child3.Value.TryGetBehavior<FlexBehavior>(out var child3Flex));

        Assert.Equal(1, child1Flex.Value.ZIndex); // default child z-index
        Assert.Equal(10, child2Flex.Value.ZIndex); // overridden to 10
        Assert.Equal(5, child3Flex.Value.ZIndex); // overridden to 5
    }

    [Fact]
    public void FlexGrowEqual_DistributesSpaceEvenly()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-grow-equal.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child3", out var child3));

        RecalculateAll();

        // Assert - all children should have roughly equal width (500/3 ≈ 166.67)
        // Yoga uses integer layout, so values will be rounded (e.g., 167, 167, 166)
        Assert.True(child1.Value.TryGetBehavior<FlexBehavior>(out var child1Flex));
        Assert.True(child2.Value.TryGetBehavior<FlexBehavior>(out var child2Flex));
        Assert.True(child3.Value.TryGetBehavior<FlexBehavior>(out var child3Flex));

        // Check that widths sum to container width (500)
        var totalWidth = child1Flex.Value.PaddingRect.Width +
                        child2Flex.Value.PaddingRect.Width +
                        child3Flex.Value.PaddingRect.Width;
        Assert.True(
            MathF.Abs(500f - totalWidth) < Tolerance,
            $"Total width should be 500, got {totalWidth}"
        );

        // Check each child is approximately 166.67 (within 1 pixel due to rounding)
        var expectedWidth = 500f / 3f;
        Assert.True(
            MathF.Abs(expectedWidth - child1Flex.Value.PaddingRect.Width) < 1f,
            $"child1 width should be ~{expectedWidth}, got {child1Flex.Value.PaddingRect.Width}"
        );
        Assert.True(
            MathF.Abs(expectedWidth - child2Flex.Value.PaddingRect.Width) < 1f,
            $"child2 width should be ~{expectedWidth}, got {child2Flex.Value.PaddingRect.Width}"
        );
        Assert.True(
            MathF.Abs(expectedWidth - child3Flex.Value.PaddingRect.Width) < 1f,
            $"child3 width should be ~{expectedWidth}, got {child3Flex.Value.PaddingRect.Width}"
        );
    }

    [Fact]
    public void FlexGrowMixed_DistributesSpaceCorrectly()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-grow-mixed.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("fixed-child", out var fixedChild));
        Assert.True(EntityIdRegistry.Instance.TryResolve("growing-child", out var growingChild));
        Assert.True(EntityIdRegistry.Instance.TryResolve("another-fixed", out var anotherFixed));

        RecalculateAll();

        // Assert - fixed children stay 100px, growing child fills remainder
        Assert.True(fixedChild.Value.TryGetBehavior<FlexBehavior>(out var fixedFlex));
        Assert.True(growingChild.Value.TryGetBehavior<FlexBehavior>(out var growingFlex));
        Assert.True(anotherFixed.Value.TryGetBehavior<FlexBehavior>(out var anotherFixedFlex));

        Assert.True(
            MathF.Abs(100 - fixedFlex.Value.PaddingRect.Width) < Tolerance,
            $"fixed-child should be 100px, got {fixedFlex.Value.PaddingRect.Width}"
        );
        Assert.True(
            MathF.Abs(200 - growingFlex.Value.PaddingRect.Width) < Tolerance,
            $"growing-child should fill 200px, got {growingFlex.Value.PaddingRect.Width}"
        );
        Assert.True(
            MathF.Abs(100 - anotherFixedFlex.Value.PaddingRect.Width) < Tolerance,
            $"another-fixed should be 100px, got {anotherFixedFlex.Value.PaddingRect.Width}"
        );
    }

    [Fact]
    public void FlexShrink_ShrinksChildrenProportionally()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-shrink.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Assert - children should shrink to fit in 200px container
        Assert.True(child1.Value.TryGetBehavior<FlexBehavior>(out var child1Flex));
        Assert.True(child2.Value.TryGetBehavior<FlexBehavior>(out var child2Flex));

        // Total should not exceed container width
        var totalWidth = child1Flex.Value.PaddingRect.Width + child2Flex.Value.PaddingRect.Width;
        Assert.True(
            totalWidth <= 200 + Tolerance,
            $"Total width should not exceed 200px, got {totalWidth}"
        );

        // child2 (shrink:2) should shrink more than child1 (shrink:1)
        Assert.True(
            child2Flex.Value.PaddingRect.Width < child1Flex.Value.PaddingRect.Width,
            $"child2 ({child2Flex.Value.PaddingRect.Width}) should be smaller than child1 ({child1Flex.Value.PaddingRect.Width})"
        );
    }

    [Fact]
    public void FlexWrapMultiRow_WrapsToMultipleRows()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-wrap-multi-row.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child3", out var child3));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child4", out var child4));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child5", out var child5));

        RecalculateAll();

        // Assert - children should wrap to multiple rows
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1Transform));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));
        Assert.True(child3.Value.TryGetBehavior<TransformBehavior>(out var child3Transform));
        Assert.True(child4.Value.TryGetBehavior<TransformBehavior>(out var child4Transform));
        Assert.True(child5.Value.TryGetBehavior<TransformBehavior>(out var child5Transform));

        // First row: child1, child2, child3
        Assert.True(
            child1Transform.Value.Position.Y < 60,
            $"child1 should be on first row, Y={child1Transform.Value.Position.Y}"
        );
        Assert.True(
            child2Transform.Value.Position.Y < 60,
            $"child2 should be on first row, Y={child2Transform.Value.Position.Y}"
        );
        Assert.True(
            child3Transform.Value.Position.Y < 60,
            $"child3 should be on first row, Y={child3Transform.Value.Position.Y}"
        );

        // Second row: child4, child5
        Assert.True(
            child4Transform.Value.Position.Y > 60,
            $"child4 should be on second row, Y={child4Transform.Value.Position.Y}"
        );
        Assert.True(
            child5Transform.Value.Position.Y > 60,
            $"child5 should be on second row, Y={child5Transform.Value.Position.Y}"
        );
    }



    [Fact]
    public void FlexBehaviorAdded_AutomaticallyAddsTransformBehavior()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-single-root.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("single-flex", out var singleFlex));

        // Assert - TransformBehavior should be automatically added
        Assert.True(
            singleFlex.Value.HasBehavior<TransformBehavior>(),
            "FlexBehavior should automatically add TransformBehavior"
        );
    }

    [Fact]
    public void FlexWithTransformRoot_FlexOverwritesTransform()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-with-transform-root.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("root", out var root));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        RecalculateAll();

        // Assert - FlexBehavior OVERRIDES Transform.Position even for root nodes
        // Root entity has Transform set to (100, 200) in JSON, but FlexBehavior overrides it to (0, 0)
        // SteffenBlake's directive: "YES even 'root' nodes with FlexBehavior have to overwrite their transform"
        Assert.True(root.Value.TryGetBehavior<TransformBehavior>(out var rootTransform));
        Assert.True(
            MathF.Abs(0 - rootTransform.Value.Position.X) < Tolerance,
            $"root X should be 0 (flex overrides transform), got {rootTransform.Value.Position.X}"
        );
        Assert.True(
            MathF.Abs(0 - rootTransform.Value.Position.Y) < Tolerance,
            $"root Y should be 0 (flex overrides transform), got {rootTransform.Value.Position.Y}"
        );

        // Child is at flex position (0, 0) relative to root (which is at 0, 0)
        AssertPositionEquals(0, 0, child.Value, "child");
    }

    [Fact]
    public void FlexNestedWithTransform_FlexOverridesTransformInNestedHierarchy()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-nested-with-transform.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("root", out var root));
        Assert.True(EntityIdRegistry.Instance.TryResolve("left-panel", out var leftPanel));
        Assert.True(EntityIdRegistry.Instance.TryResolve("left-item1", out var leftItem1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("left-item2", out var leftItem2));
        Assert.True(EntityIdRegistry.Instance.TryResolve("right-panel", out var rightPanel));

        RecalculateAll();

        // Assert - FlexBehavior overrides Transform even for root nodes
        // Root has transform (50, 50) in JSON, but FlexBehavior overrides it to (0, 0)
        // SteffenBlake's directive: "YES even 'root' nodes with FlexBehavior have to overwrite their transform"
        AssertWorldPositionEquals(0, 0, leftPanel.Value, "left-panel");
        AssertWorldPositionEquals(200, 0, rightPanel.Value, "right-panel");

        // Nested items under left-panel (which is at 0, 0)
        AssertWorldPositionEquals(0, 0, leftItem1.Value, "left-item1");
        AssertWorldPositionEquals(0, 100, leftItem2.Value, "left-item2");
    }



    [Fact]
    public void FlexEmptyContainer_HandlesGracefully()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-empty-container.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));

        RecalculateAll();

        // Assert - container should have flex behavior even without children
        Assert.True(
            container.Value.HasBehavior<FlexBehavior>(),
            "Empty container should have FlexBehavior"
        );
        Assert.True(
            container.Value.HasBehavior<TransformBehavior>(),
            "Empty container should have TransformBehavior"
        );
    }

    [Fact]
    public void FlexSingleRoot_WorksWithoutParent()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-single-root.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("single-flex", out var singleFlex));

        RecalculateAll();

        // Assert - single root flex entity should work
        Assert.True(singleFlex.Value.HasBehavior<FlexBehavior>());
        AssertPositionEquals(0, 0, singleFlex.Value, "single-flex");
    }

    [Fact]
    public void FlexDisabledContainer_HidesFromLayout()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/flex-disabled-container.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("disabled-container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child", out var child));

        // Manually disable the container to test FlexRegistry's EntityDisabledEvent handler
        container.Value.Disable();

        RecalculateAll();

        // Assert - disabled entities should be marked as Display.None
        // This is handled by FlexRegistry's EntityDisabledEvent handler
        Assert.False(
            container.Value.Enabled,
            "Container should be disabled"
        );
    }

    [Fact]
    public void FlexRecalculate_UpdatesAfterBehaviorChange()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/simple-flex-row.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Verify initial positions
        AssertPositionEquals(0, 0, child1.Value, "child1 initially");
        AssertPositionEquals(100, 0, child2.Value, "child2 initially");

        // Modify child1 width
        var newWidth = 200f;
        child1.Value.SetBehavior<FlexWidthBehavior, float>(
            in newWidth,
            static (ref readonly width, ref behavior) => behavior = behavior with
            {
                Value = width
            }
        );

        RecalculateAll();

        // Assert - child2 should move to accommodate child1's new width
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2Transform));
        Assert.True(
            child2Transform.Value.Position.X > 150,
            $"child2 should move right after child1 width change, X={child2Transform.Value.Position.X}"
        );
    }

    [Fact]
    public void FlexMultipleRecalculations_ProducesSameResults()
    {
        // Arrange
        var scenePath = "Flex/Fixtures/simple-flex-row.json";

        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        Assert.True(EntityIdRegistry.Instance.TryResolve("container", out var container));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child1", out var child1));
        Assert.True(EntityIdRegistry.Instance.TryResolve("child2", out var child2));

        RecalculateAll();

        // Get positions after first recalculation
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1TransformFirst));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2TransformFirst));
        var child1PosFirst = child1TransformFirst.Value.Position;
        var child2PosFirst = child2TransformFirst.Value.Position;

        // Recalculate multiple times
        RecalculateAll();
        RecalculateAll();
        RecalculateAll();

        // Assert - positions should be identical
        Assert.True(child1.Value.TryGetBehavior<TransformBehavior>(out var child1TransformFinal));
        Assert.True(child2.Value.TryGetBehavior<TransformBehavior>(out var child2TransformFinal));

        Assert.True(
            MathF.Abs(child1PosFirst.X - child1TransformFinal.Value.Position.X) < Tolerance,
            $"child1 X should be stable across recalculations"
        );
        Assert.True(
            MathF.Abs(child1PosFirst.Y - child1TransformFinal.Value.Position.Y) < Tolerance,
            $"child1 Y should be stable across recalculations"
        );
        Assert.True(
            MathF.Abs(child2PosFirst.X - child2TransformFinal.Value.Position.X) < Tolerance,
            $"child2 X should be stable across recalculations"
        );
        Assert.True(
            MathF.Abs(child2PosFirst.Y - child2TransformFinal.Value.Position.Y) < Tolerance,
            $"child2 Y should be stable across recalculations"
        );
    }


    [Fact]
    public void RapidParentChanges_CleansUpAllIntermediateParents()
    {
        // Arrange - Test BUG-01 fix
        // Create entities A, B, C, and child that will change parents A→B→C→NULL
        var parentA = EntityRegistry.Instance.Activate();
        var parentB = EntityRegistry.Instance.Activate();
        var parentC = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate();

        // Set IDs for parent resolution
        parentA.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parentA" });
        parentB.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parentB" });
        parentC.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parentC" });
        child.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "child" });

        // Add FlexBehavior to all parents so hierarchy is relevant
        parentA.SetBehavior<FlexBehavior>(static (ref _) => { });
        parentB.SetBehavior<FlexBehavior>(static (ref _) => { });
        parentC.SetBehavior<FlexBehavior>(static (ref _) => { });
        child.SetBehavior<FlexBehavior>(static (ref _) => { });

        // Act - Rapidly change parent: A → B → C → NULL
        Assert.True(SelectorRegistry.Instance.TryParse("@parentA", out var selectorA));
        child.SetBehavior<ParentBehavior, EntitySelector>(
            in selectorA,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();

        Assert.True(SelectorRegistry.Instance.TryParse("@parentB", out var selectorB));
        child.SetBehavior<ParentBehavior, EntitySelector>(
            in selectorB,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();

        Assert.True(SelectorRegistry.Instance.TryParse("@parentC", out var selectorC));
        child.SetBehavior<ParentBehavior, EntitySelector>(
            in selectorC,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();

        child.RemoveBehavior<ParentBehavior>();
        HierarchyRegistry.Instance.Recalc();

        // Assert - ALL intermediate parents (A, B, C) should have child removed
        Assert.False(
            HierarchyRegistry.Instance.HasChild(parentA, child),
            "Child should be removed from parentA"
        );
        Assert.False(
            HierarchyRegistry.Instance.HasChild(parentB, child),
            "Child should be removed from parentB"
        );
        Assert.False(
            HierarchyRegistry.Instance.HasChild(parentC, child),
            "Child should be removed from parentC"
        );
        Assert.False(
            child.TryGetParent(out _),
            "Child should have no parent"
        );
    }

    [Fact]
    public void RemoveFlexBehavior_MarksParentDirty()
    {
        // Arrange - Test BUG-02 fix: parent with 2 FlexGrow children
        // Setup so removing a child actually changes layout dimensions (provable recalculation)
        var parent = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate();
        var child2 = EntityRegistry.Instance.Activate();

        parent.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parent" });
        child1.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "child1" });
        child2.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "child2" });

        // Parent: 400x200 container with Row direction
        parent.SetBehavior<FlexBehavior>(static (ref _) => { });
        var parentWidth = 400f;
        parent.SetBehavior<FlexWidthBehavior, float>(
            in parentWidth,
            static (ref readonly width, ref behavior) => behavior = new FlexWidthBehavior(width, false)
        );
        var parentHeight = 200f;
        parent.SetBehavior<FlexHeightBehavior, float>(
            in parentHeight,
            static (ref readonly height, ref behavior) => behavior = new FlexHeightBehavior(height, false)
        );
        parent.SetBehavior<FlexDirectionBehavior>(
            static (ref behavior) => behavior = new FlexDirectionBehavior(FlexDirection.Row)
        );

        // Child1: FlexGrow=1, should get half of parent width (200px)
        Assert.True(SelectorRegistry.Instance.TryParse("@parent", out var parentSelector));
        child1.SetBehavior<ParentBehavior, EntitySelector>(
            in parentSelector,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        child1.SetBehavior<FlexBehavior>(static (ref _) => { });
        child1.SetBehavior<FlexGrowBehavior>(
            static (ref behavior) => behavior = new FlexGrowBehavior(1)
        );
        var child1Height = 100f;
        child1.SetBehavior<FlexHeightBehavior, float>(
            in child1Height,
            static (ref readonly height, ref behavior) => behavior = new FlexHeightBehavior(height, false)
        );

        // Child2: FlexGrow=1, should get half of parent width (200px)
        child2.SetBehavior<ParentBehavior, EntitySelector>(
            in parentSelector,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        child2.SetBehavior<FlexBehavior>(static (ref _) => { });
        child2.SetBehavior<FlexGrowBehavior>(
            static (ref behavior) => behavior = new FlexGrowBehavior(1)
        );
        var child2Height = 100f;
        child2.SetBehavior<FlexHeightBehavior, float>(
            in child2Height,
            static (ref readonly height, ref behavior) => behavior = new FlexHeightBehavior(height, false)
        );

        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();
        RecalculateAll();

        // Both children should each get ~200px width (400/2)
        Assert.True(child1.TryGetBehavior<FlexBehavior>(out var child1FlexBefore));
        Assert.True(child2.TryGetBehavior<FlexBehavior>(out var child2FlexBefore));

        var child1WidthBefore = child1FlexBefore.Value.ContentRect.Width;
        var child2WidthBefore = child2FlexBefore.Value.ContentRect.Width;

        Assert.True(
            MathF.Abs(child1WidthBefore - 200) < 1,
            $"Child1 width should be ~200, got {child1WidthBefore}"
        );
        Assert.True(
            MathF.Abs(child2WidthBefore - 200) < 1,
            $"Child2 width should be ~200, got {child2WidthBefore}"
        );

        // Act - Remove child1's FlexBehavior
        child1.RemoveBehavior<FlexBehavior>();
        RecalculateAll();

        // Assert - child2 should now take full width (400px) because child1 is gone
        // This proves parent was marked dirty and recalculated
        Assert.True(
            child2.TryGetBehavior<FlexBehavior>(out var child2FlexAfter),
            "Child2 should still have FlexBehavior"
        );

        var child2WidthAfter = child2FlexAfter.Value.ContentRect.Width;

        Assert.True(
            MathF.Abs(child2WidthAfter - 400) < 1,
            $"Child2 should grow to full parent width (400px) after child1 removed, got {child2WidthAfter}"
        );
    }

    [Fact]
    public void ParentChangeDuringRecalculate_ProcessedCorrectly()
    {
        // Arrange - Test parent changes during recalculate
        var parentA = EntityRegistry.Instance.Activate();
        var parentB = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate();

        parentA.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parentA" });
        parentB.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parentB" });
        child.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "child" });

        parentA.SetBehavior<FlexBehavior>(static (ref _) => { });
        parentB.SetBehavior<FlexBehavior>(static (ref _) => { });
        child.SetBehavior<FlexBehavior>(static (ref _) => { });

        Assert.True(SelectorRegistry.Instance.TryParse("@parentA", out var selectorA));
        child.SetBehavior<ParentBehavior, EntitySelector>(
            in selectorA,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();
        RecalculateAll();

        // Act - Change parent and recalculate
        Assert.True(SelectorRegistry.Instance.TryParse("@parentB", out var selectorB));
        child.SetBehavior<ParentBehavior, EntitySelector>(
            in selectorB,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();
        RecalculateAll();

        // Assert - Child should be under new parent
        Assert.True(child.TryGetParent(out var parent));
        Assert.Equal(parentB.Index, parent.Value.Index);
        Assert.True(HierarchyRegistry.Instance.HasChild(parentB, child));
        Assert.False(HierarchyRegistry.Instance.HasChild(parentA, child));
    }

    [Fact]
    public void DisabledEntity_CanChangeParent_ThenReEnable()
    {
        // Arrange - Test disable → change parent → re-enable workflow
        var parentA = EntityRegistry.Instance.Activate();
        var parentB = EntityRegistry.Instance.Activate();
        var child = EntityRegistry.Instance.Activate();

        parentA.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parentA" });
        parentB.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parentB" });
        child.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "child" });

        parentA.SetBehavior<FlexBehavior>(static (ref _) => { });
        parentB.SetBehavior<FlexBehavior>(static (ref _) => { });
        child.SetBehavior<FlexBehavior>(static (ref _) => { });

        Assert.True(SelectorRegistry.Instance.TryParse("@parentA", out var selectorA));
        child.SetBehavior<ParentBehavior, EntitySelector>(
            in selectorA,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();
        RecalculateAll();

        // Act - Disable, change parent, re-enable
        child.Disable();
        RecalculateAll();

        Assert.True(SelectorRegistry.Instance.TryParse("@parentB", out var selectorB));
        child.SetBehavior<ParentBehavior, EntitySelector>(
            in selectorB,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();

        child.Enable();
        RecalculateAll();

        // Assert - Child should be under new parent after re-enable
        Assert.True(child.Enabled);
        Assert.True(child.TryGetParent(out var parent));
        Assert.Equal(parentB.Index, parent.Value.Index);
        Assert.True(HierarchyRegistry.Instance.HasChild(parentB, child));
    }

    [Fact]
    public void ParentDeactivation_OrphansFlexChildren()
    {
        // Arrange - Test parent deactivation orphans children
        var parent = EntityRegistry.Instance.Activate();
        var child1 = EntityRegistry.Instance.Activate();
        var child2 = EntityRegistry.Instance.Activate();

        parent.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "parent" });
        child1.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "child1" });
        child2.SetBehavior<IdBehavior>(static (ref b) => b = b with { Id = "child2" });

        parent.SetBehavior<FlexBehavior>(static (ref _) => { });
        child1.SetBehavior<FlexBehavior>(static (ref _) => { });
        child2.SetBehavior<FlexBehavior>(static (ref _) => { });

        Assert.True(SelectorRegistry.Instance.TryParse("@parent", out var parentSelector));
        child1.SetBehavior<ParentBehavior, EntitySelector>(
            in parentSelector,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        child2.SetBehavior<ParentBehavior, EntitySelector>(
            in parentSelector,
            static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
        );
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();
        RecalculateAll();

        // Act - Deactivate parent
        EntityRegistry.Instance.Deactivate(parent);

        // Assert - Children should be orphaned
        Assert.False(child1.TryGetParent(out _), "child1 should be orphaned");
        Assert.False(child2.TryGetParent(out _), "child2 should be orphaned");
    }

    [Fact]
    public void DeeplyNestedHierarchy_NoStackOverflow()
    {
        // Arrange - Stress test with 12-level deep hierarchy
        var entities = new Entity[12];
        for (int i = 0; i < 12; i++)
        {
            entities[i] = EntityRegistry.Instance.Activate();
            entities[i].SetBehavior<IdBehavior, int>(
                in i,
                static (ref readonly index, ref b) => b = b with { Id = $"entity{index}" }
            );
            entities[i].SetBehavior<FlexBehavior>(static (ref _) => { });
        }

        // Create hierarchy: entity0 → entity1 → entity2 → ... → entity11
        for (int i = 1; i < 12; i++)
        {
            var parentId = $"@entity{i - 1}";
            Assert.True(SelectorRegistry.Instance.TryParse(parentId, out var selector));
            entities[i].SetBehavior<ParentBehavior, EntitySelector>(
                in selector,
                static (ref readonly sel, ref behavior) => behavior = new ParentBehavior(sel)
            );
        }

        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();

        // Act - Should not cause stack overflow
        RecalculateAll();

        // Assert - Verify hierarchy is intact
        for (int i = 1; i < 12; i++)
        {
            Assert.True(entities[i].TryGetParent(out var parent));
            Assert.Equal(entities[i - 1].Index, parent.Value.Index);
        }

        // Verify deepest child has correct hierarchy
        Assert.True(entities[11].TryGetParent(out var parent11));
        Assert.Equal(entities[10].Index, parent11.Value.Index);
    }


}
