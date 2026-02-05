using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Tests.Selectors.Units;

/// <summary>
/// Unit tests for EntitySelector parsing via SelectorRegistry.
/// Tests individual selector parsing, operator precedence, and error handling.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class SelectorParsingUnitTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public SelectorParsingUnitTests(ITestOutputHelper output)
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

    [Fact]
    public void TryParse_WithSingleIdSelector_ParsesCorrectly()
    {
        // Arrange
        var input = "@player";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.True(success, "Should successfully parse @player");
        Assert.NotNull(selector);

        // test-architect: Verify it's an IdEntitySelector variant by checking ToString
        Assert.True(selector.TryMatch(out IdEntitySelector? idSelector), "Should be IdEntitySelector");
        Assert.NotNull(idSelector);
        Assert.Equal("@player", selector.ToString());
    }

    [Fact]
    public void TryParse_WithSingleTagSelector_ParsesCorrectly()
    {
        // Arrange
        var input = "#enemies";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.True(success, "Should successfully parse #enemies");
        Assert.NotNull(selector);

        // test-architect: Verify it's a TagEntitySelector variant by checking ToString
        Assert.True(selector.TryMatch(out TagEntitySelector? tagSelector), "Should be TagEntitySelector");
        Assert.NotNull(tagSelector);
        Assert.Equal("#enemies", selector.ToString());
    }

    [Fact]
    public void TryParse_WithCollisionEnterSelector_ParsesCorrectly()
    {
        // Arrange
        var input = "!enter";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.True(success, "Should successfully parse !enter");
        Assert.NotNull(selector);

        // test-architect: Verify it's a CollisionEnterEntitySelector variant
        Assert.True(selector.TryMatch(out CollisionEnterEntitySelector? enterSelector), "Should be CollisionEnterEntitySelector");
        Assert.NotNull(enterSelector);
        Assert.Equal("!enter", selector.ToString());
    }

    [Fact]
    public void TryParse_WithCollisionExitSelector_ParsesCorrectly()
    {
        // Arrange
        var input = "!exit";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.True(success, "Should successfully parse !exit");
        Assert.NotNull(selector);

        // test-architect: Verify it's a CollisionExitEntitySelector variant
        Assert.True(selector.TryMatch(out CollisionExitEntitySelector? exitSelector), "Should be CollisionExitEntitySelector");
        Assert.NotNull(exitSelector);
        Assert.Equal("!exit", selector.ToString());
    }

    [Fact]
    public void TryParse_WithUnionOfTwoIds_ParsesCorrectly()
    {
        // Arrange
        var input = "@player,@boss";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.True(success, "Should successfully parse @player,@boss");
        Assert.NotNull(selector);

        // test-architect: Verify it's a UnionEntitySelector variant
        Assert.True(selector.TryMatch(out UnionEntitySelector? unionSelector), "Should be UnionEntitySelector");
        Assert.NotNull(unionSelector);
        // test-architect: Verify union toString matches expected format
        Assert.Equal("@player,@boss", selector.ToString());
    }

    [Fact]
    public void TryParse_WithRefinementChain_ParsesCorrectly()
    {
        // Arrange
        var input = "!enter:#enemies";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.True(success, "Should successfully parse !enter:#enemies");
        Assert.NotNull(selector);

        // senior-dev: Per sprint doc: "!enter:#enemies" → Tag("enemies", Prior: CollisionEnter())
        // The rightmost selector is the root (Tag), leftmost is its prior (CollisionEnter)
        Assert.True(selector.TryMatch(out TagEntitySelector? tagSelector), "Should be TagEntitySelector");
        Assert.NotNull(tagSelector);
        // senior-dev: Verify toString shows input format
        Assert.Equal("!enter:#enemies", selector.ToString());
    }

    [Fact]
    public void TryParse_WithMultipleRefinements_ParsesCorrectly()
    {
        // Arrange
        var input = "!enter:#enemies:#boss";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.True(success, "Should successfully parse !enter:#enemies:#boss");
        Assert.NotNull(selector);

        // test-architect: Verify toString shows input format (left-to-right)
        Assert.Equal("!enter:#enemies:#boss", selector.ToString());
    }

    [Fact]
    public void TryParse_WithComplexPrecedence_ParsesCorrectly()
    {
        // Arrange
        var input = "@player,!enter:#enemies:#boss";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.True(success, "Should successfully parse @player,!enter:#enemies:#boss");
        Assert.NotNull(selector);

        // test-architect: Verify it's a UnionEntitySelector
        Assert.True(selector.TryMatch(out UnionEntitySelector? unionSelector), "Should be UnionEntitySelector");
        Assert.NotNull(unionSelector);
        // test-architect: Verify toString shows input format (left-to-right)
        Assert.Equal("@player,!enter:#enemies:#boss", selector.ToString());
    }

    [Fact]
    public void TryParse_WithWhitespace_FiresErrorEvent()
    {
        // Arrange
        var input = "@player , @boss";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        // senior-dev: Whitespace is an invalid character, should fire error
        Assert.False(success, "Should fail to parse @player , @boss with whitespace");
        Assert.Null(selector);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent for whitespace");
    }

    [Fact]
    public void TryParse_WithWhitespaceInRefinement_FiresErrorEvent()
    {
        // Arrange
        var input = "!enter : #enemies";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.False(success, "Should fail to parse !enter : #enemies with whitespace in refinement");
        Assert.Null(selector);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent for whitespace in refinement");
    }

    [Fact]
    public void TryParse_WithDoubleAtSign_FiresErrorEvent()
    {
        // Arrange
        var input = "@@player";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.False(success, "Should fail to parse @@player");
        Assert.Null(selector);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent");
    }

    [Fact]
    public void TryParse_WithUnknownPrefix_FiresErrorEvent()
    {
        // Arrange
        var input = "!unknown";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.False(success, "Should fail to parse !unknown");
        Assert.Null(selector);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent");
    }

    [Fact]
    public void TryParse_WithAtSignOnly_FiresErrorEvent()
    {
        // Arrange
        var input = "@";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.False(success, "Should fail to parse @");
        Assert.Null(selector);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent");
    }

    [Fact]
    public void TryParse_WithHashOnly_FiresErrorEvent()
    {
        // Arrange
        var input = "#";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.False(success, "Should fail to parse #");
        Assert.Null(selector);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent");
    }

    [Fact]
    public void TryParse_WithEmptyString_FiresErrorEvent()
    {
        // Arrange
        var input = "";

        // Act
        var success = SelectorRegistry.Instance.TryParse(input, out var selector);

        // Assert
        Assert.False(success, "Should fail to parse empty string");
        Assert.Null(selector);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent");
    }
}
