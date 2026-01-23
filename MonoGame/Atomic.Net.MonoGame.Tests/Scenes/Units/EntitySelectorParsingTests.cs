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
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse @id selector");
        Assert.NotNull(parsed);
        
        // test-architect: Use TryMatch to verify it's an IdEntitySelector
        var isId = parsed.Value.TryMatch(out IdEntitySelector idSelector);
        Assert.True(isId, "Parsed selector should be IdEntitySelector");
        Assert.Equal("player", idSelector.Id);
        Assert.Null(idSelector.Next);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithTagSelector_ReturnsTaggedEntitySelector()
    {
        // Arrange
        var selector = "#poisoned";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse #tag selector");
        Assert.NotNull(parsed);
        
        // test-architect: Use TryMatch to verify it's a TaggedEntitySelector
        var isTagged = parsed.Value.TryMatch(out TaggedEntitySelector taggedSelector);
        Assert.True(isTagged, "Parsed selector should be TaggedEntitySelector");
        Assert.Equal("poisoned", taggedSelector.Tag);
        Assert.Null(taggedSelector.Next);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithCollisionEnter_ReturnsCollisionEnterEntitySelector()
    {
        // Arrange
        var selector = "!enter";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse !enter selector");
        Assert.NotNull(parsed);
        
        // test-architect: Use TryMatch to verify it's a CollisionEnterEntitySelector
        var isEnter = parsed.Value.TryMatch(out CollisionEnterEntitySelector enterSelector);
        Assert.True(isEnter, "Parsed selector should be CollisionEnterEntitySelector");
        Assert.Null(enterSelector.Next);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithCollisionExit_ReturnsCollisionExitEntitySelector()
    {
        // Arrange
        var selector = "!exit";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse !exit selector");
        Assert.NotNull(parsed);
        
        // test-architect: Use TryMatch to verify it's a CollisionExitEntitySelector
        var isExit = parsed.Value.TryMatch(out CollisionExitEntitySelector exitSelector);
        Assert.True(isExit, "Parsed selector should be CollisionExitEntitySelector");
        Assert.Null(exitSelector.Next);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithUnion_ReturnsUnionEntitySelector()
    {
        // Arrange
        var selector = "@player, @boss";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse union selector");
        Assert.NotNull(parsed);
        
        // test-architect: Use TryMatch to verify it's a UnionEntitySelector
        var isUnion = parsed.Value.TryMatch(out UnionEntitySelector unionSelector);
        Assert.True(isUnion, "Parsed selector should be UnionEntitySelector");
        Assert.Equal(2, unionSelector.Children.Length);
        
        // test-architect: Verify first child is @player
        Assert.True(unionSelector.Children[0].TryMatch(out IdEntitySelector firstId));
        Assert.Equal("player", firstId.Id);
        
        // test-architect: Verify second child is @boss
        Assert.True(unionSelector.Children[1].TryMatch(out IdEntitySelector secondId));
        Assert.Equal("boss", secondId.Id);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithRefinementChain_ReturnsChainedSelectors()
    {
        // Arrange
        var selector = "!enter:#enemies";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse refinement chain");
        Assert.NotNull(parsed);
        
        // test-architect: Use TryMatch to verify it's a CollisionEnterEntitySelector
        var isEnter = parsed.Value.TryMatch(out CollisionEnterEntitySelector enterSelector);
        Assert.True(isEnter, "Root should be CollisionEnterEntitySelector");
        Assert.NotNull(enterSelector.Next);
        
        // test-architect: Verify Next is TaggedEntitySelector with "enemies"
        var isTagged = enterSelector.Next.Value.TryMatch(out TaggedEntitySelector taggedSelector);
        Assert.True(isTagged, "Next should be TaggedEntitySelector");
        Assert.Equal("enemies", taggedSelector.Tag);
        Assert.Null(taggedSelector.Next);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithMultipleRefinements_ReturnsDeepChain()
    {
        // Arrange
        var selector = "#tag1:#tag2";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse multiple refinements");
        Assert.NotNull(parsed);
        
        // test-architect: Use TryMatch to verify first level
        var isFirstTag = parsed.Value.TryMatch(out TaggedEntitySelector firstTag);
        Assert.True(isFirstTag, "Root should be TaggedEntitySelector");
        Assert.Equal("tag1", firstTag.Tag);
        Assert.NotNull(firstTag.Next);
        
        // test-architect: Verify second level
        var isSecondTag = firstTag.Next.Value.TryMatch(out TaggedEntitySelector secondTag);
        Assert.True(isSecondTag, "Next should be TaggedEntitySelector");
        Assert.Equal("tag2", secondTag.Tag);
        Assert.Null(secondTag.Next);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithPrecedence_UnionOfRefinementChains()
    {
        // Arrange
        // test-architect: Precedence rule: `:` binds tighter than `,`
        // Expected parse: Union([@player, CollisionEnter(Next: Tagged("enemies", Next: Tagged("boss")))])
        var selector = "@player, !enter:#enemies:#boss";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse complex precedence");
        Assert.NotNull(parsed);
        
        // test-architect: Root should be UnionEntitySelector
        var isUnion = parsed.Value.TryMatch(out UnionEntitySelector unionSelector);
        Assert.True(isUnion, "Root should be UnionEntitySelector");
        Assert.Equal(2, unionSelector.Children.Length);
        
        // test-architect: First child should be @player (simple IdEntitySelector)
        Assert.True(unionSelector.Children[0].TryMatch(out IdEntitySelector playerId));
        Assert.Equal("player", playerId.Id);
        Assert.Null(playerId.Next);
        
        // test-architect: Second child should be !enter with chain
        Assert.True(unionSelector.Children[1].TryMatch(out CollisionEnterEntitySelector enterSelector));
        Assert.NotNull(enterSelector.Next);
        
        // test-architect: Next should be #enemies
        Assert.True(enterSelector.Next.Value.TryMatch(out TaggedEntitySelector enemiesTag));
        Assert.Equal("enemies", enemiesTag.Tag);
        Assert.NotNull(enemiesTag.Next);
        
        // test-architect: Next should be #boss
        Assert.True(enemiesTag.Next.Value.TryMatch(out TaggedEntitySelector bossTag));
        Assert.Equal("boss", bossTag.Tag);
        Assert.Null(bossTag.Next);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithInvalidDoubleAt_ReturnsFalseAndFiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var selector = "@@invalid";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.False(success, "Should fail to parse @@invalid");
        Assert.Null(parsed);
        
        // test-architect: Should fire ErrorEvent containing "@@" or "invalid" or "parse"
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithUnknownEvent_ReturnsFalseAndFiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var selector = "!unknown";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.False(success, "Should fail to parse !unknown");
        Assert.Null(parsed);
        
        // test-architect: Should fire ErrorEvent for unknown event type
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithSolitaryAt_ReturnsFalseAndFiresErrorEvent()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var selector = "@";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
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
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
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
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
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
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse union with whitespace");
        Assert.NotNull(parsed);
        
        // test-architect: Should still be a UnionEntitySelector
        var isUnion = parsed.Value.TryMatch(out UnionEntitySelector unionSelector);
        Assert.True(isUnion);
        Assert.Equal(2, unionSelector.Children.Length);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithWhitespaceInRefinement_ParsesCorrectly()
    {
        // Arrange
        // test-architect: Whitespace around refinement operator should be handled
        var selector = "!enter : #enemies";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse refinement with whitespace");
        Assert.NotNull(parsed);
        
        // test-architect: Should still be CollisionEnterEntitySelector with Next
        var isEnter = parsed.Value.TryMatch(out CollisionEnterEntitySelector enterSelector);
        Assert.True(isEnter);
        Assert.NotNull(enterSelector.Next);
    }

    [Fact(Skip = "Waiting for @senior-dev to implement EntitySelectorV2.TryParse")]
    public void TryParse_WithLeadingTrailingWhitespace_ParsesCorrectly()
    {
        // Arrange
        var selector = "  @player  ";

        // Act
        // test-architect: Change EntitySelectorV2 → EntitySelector after refactor
        var success = EntitySelectorV2.TryParse(selector, out var parsed);

        // Assert
        Assert.True(success, "Should successfully parse with leading/trailing whitespace");
        Assert.NotNull(parsed);
        
        // test-architect: Should be IdEntitySelector with "player"
        var isId = parsed.Value.TryMatch(out IdEntitySelector idSelector);
        Assert.True(isId);
        Assert.Equal("player", idSelector.Id);
    }
}
