using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;
using Xunit;

namespace Atomic.Net.MonoGame.Tests.Scenes.Units;

/// <summary>
/// Unit tests for EntitySelectorV2.TryParse() method.
/// Tests the parser with various selector syntaxes, precedence rules, and error cases.
/// 
/// test-architect: Currently using EntitySelectorV2. Once @senior-dev completes the migration
/// and renames EntitySelectorV2 to EntitySelector, update all references in this file.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class EntitySelectorParsingTests : IDisposable
{
    public EntitySelectorParsingTests()
    {
        // Arrange: Initialize minimal systems for parsing tests
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up after each test
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithIdSelector_ReturnsIdEntitySelector()
    {
        // Arrange
        var selector = "@player";

        // Act
        // test-architect: This will throw NotImplementedException until @senior-dev implements it
        // Once implemented, it should return a parsed EntitySelectorV2 (variant union type)
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, verify it's an IdEntitySelector with id="player"
        Assert.True(success, "Should successfully parse @id selector");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithTagSelector_ReturnsTaggedEntitySelector()
    {
        // Arrange
        var selector = "#poisoned";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, verify it's a TaggedEntitySelector with tag="poisoned"
        Assert.True(success, "Should successfully parse #tag selector");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithCollisionEnter_ReturnsCollisionEnterEntitySelector()
    {
        // Arrange
        var selector = "!enter";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, verify it's a CollisionEnterEntitySelector
        Assert.True(success, "Should successfully parse !enter selector");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithCollisionExit_ReturnsCollisionExitEntitySelector()
    {
        // Arrange
        var selector = "!exit";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, verify it's a CollisionExitEntitySelector
        Assert.True(success, "Should successfully parse !exit selector");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithUnion_ReturnsUnionEntitySelector()
    {
        // Arrange
        var selector = "@player, @boss";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, verify it's a UnionEntitySelector with 2 children (@player, @boss)
        Assert.True(success, "Should successfully parse union selector");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithRefinementChain_ReturnsChainedSelectors()
    {
        // Arrange
        var selector = "!enter:#enemies";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, verify CollisionEnterEntitySelector with Next=TaggedEntitySelector("enemies")
        Assert.True(success, "Should successfully parse refinement chain");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithMultipleRefinements_ReturnsDeepChain()
    {
        // Arrange
        var selector = "#tag1:#tag2";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, verify TaggedEntitySelector("tag1", Next=TaggedEntitySelector("tag2"))
        Assert.True(success, "Should successfully parse multiple refinements");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithPrecedence_UnionOfRefinementChains()
    {
        // Arrange
        // test-architect: Precedence rule: `:` binds tighter than `,`
        // Expected parse: Union([@player, CollisionEnter(Next: Tagged("enemies", Next: Tagged("boss")))])
        var selector = "@player, !enter:#enemies:#boss";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, verify Union with correct precedence:
        // Child[0] = IdEntitySelector("player")
        // Child[1] = CollisionEnterEntitySelector(Next=TaggedEntitySelector("enemies", Next=TaggedEntitySelector("boss")))
        Assert.True(success, "Should successfully parse complex precedence");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithInvalidDoubleAt_ReturnsFalseAndFiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var selector = "@@invalid";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, should return false and fire ErrorEvent
        Assert.False(success, "Should fail to parse @@invalid");
        Assert.Null(parsed);
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithUnknownEvent_ReturnsFalseAndFiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var selector = "!unknown";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, should return false and fire ErrorEvent for unknown event type
        Assert.False(success, "Should fail to parse !unknown");
        Assert.Null(parsed);
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithSolitaryAt_ReturnsFalseAndFiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var selector = "@";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, should return false and fire ErrorEvent
        Assert.False(success, "Should fail to parse solitary @");
        Assert.Null(parsed);
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithSolitaryHash_ReturnsFalseAndFiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var selector = "#";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, should return false and fire ErrorEvent
        Assert.False(success, "Should fail to parse solitary #");
        Assert.Null(parsed);
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithEmptyString_ReturnsFalseAndFiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var selector = "";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, should return false and fire ErrorEvent
        Assert.False(success, "Should fail to parse empty string");
        Assert.Null(parsed);
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithWhitespaceInUnion_ParsesCorrectly()
    {
        // Arrange
        // test-architect: Whitespace around operators should be handled gracefully
        var selector = "@player , @boss";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, should parse as UnionEntitySelector with 2 children
        Assert.True(success, "Should successfully parse union with whitespace");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithWhitespaceInRefinement_ParsesCorrectly()
    {
        // Arrange
        // test-architect: Whitespace around refinement operator should be handled
        var selector = "!enter : #enemies";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, should parse as CollisionEnterEntitySelector with Next
        Assert.True(success, "Should successfully parse refinement with whitespace");
        Assert.NotNull(parsed);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithLeadingTrailingWhitespace_ParsesCorrectly()
    {
        // Arrange
        var selector = "  @player  ";

        // Act
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        // test-architect: After implementation, should parse as IdEntitySelector with id="player"
        Assert.True(success, "Should successfully parse with leading/trailing whitespace");
        Assert.NotNull(parsed);
    }
}
