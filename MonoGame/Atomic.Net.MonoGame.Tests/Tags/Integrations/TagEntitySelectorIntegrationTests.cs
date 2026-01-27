using System.Collections.Immutable;
using Atomic.Net.MonoGame;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Tags;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Tags.Integrations;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class TagEntitySelectorIntegrationTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    
    public TagEntitySelectorIntegrationTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        EventBus<ShutdownEvent>.Push(new());
        _errorLogger.Dispose();
    }

    #region Selector Matching

    [Fact]
    public void TagEntitySelector_SingleEntityWithTag_MatchesCorrectly()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        var behavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy") };

        entity.SetBehavior<TagsBehavior, TagsBehavior>(
            in behavior,
            (ref readonly input, ref b) => b = input
        );

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("#enemy", out var selector));
        selector.Recalc();

        // Assert
        Assert.True(selector.Matches.HasValue(entity.Index));
    }

    [Fact]
    public void TagEntitySelector_MultipleEntitiesWithSameTag_MatchesAll()
    {
        // Arrange
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();
        var entity3 = EntityRegistry.Instance.Activate();
        var behavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy") };

        entity1.SetBehavior<TagsBehavior, TagsBehavior>(
            in behavior,
            (ref readonly input, ref b) => b = input
        );
        entity2.SetBehavior<TagsBehavior, TagsBehavior>(
            in behavior,
            (ref readonly input, ref b) => b = input
        );
        entity3.SetBehavior<TagsBehavior, TagsBehavior>(
            in behavior,
            (ref readonly input, ref b) => b = input
        );

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("#enemy", out var selector));
        selector.Recalc();

        // Assert
        Assert.True(selector.Matches.HasValue(entity1.Index));
        Assert.True(selector.Matches.HasValue(entity2.Index));
        Assert.True(selector.Matches.HasValue(entity3.Index));
    }

    [Fact]
    public void TagEntitySelector_EntityWithMultipleTags_MatchesBothTags()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        var behavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy", "boss") };

        entity.SetBehavior<TagsBehavior, TagsBehavior>(
            in behavior,
            (ref readonly input, ref b) => b = input
        );

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("#enemy", out var enemySelector));
        enemySelector.Recalc();

        Assert.True(SelectorRegistry.Instance.TryParse("#boss", out var bossSelector));
        bossSelector.Recalc();

        // Assert
        Assert.True(enemySelector.Matches.HasValue(entity.Index));
        Assert.True(bossSelector.Matches.HasValue(entity.Index));
    }

    [Fact]
    public void TagEntitySelector_NoEntitiesWithTag_MatchesNothing()
    {
        // Arrange - No entities created

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("#enemy", out var selector));
        selector.Recalc();

        // Assert - Matches should be empty
        var hasAnyMatches = false;
        foreach (var (_, _) in selector.Matches)
        {
            hasAnyMatches = true;
            break;
        }
        Assert.False(hasAnyMatches);
    }

    #endregion

    #region Selector Refinement

    [Fact]
    public void TagEntitySelector_RefinementWithTwoTags_MatchesOnlyEntitiesWithBoth()
    {
        // Arrange
        var enemyOnly = EntityRegistry.Instance.Activate();
        var bossOnly = EntityRegistry.Instance.Activate();
        var enemyBoss = EntityRegistry.Instance.Activate();

        var enemyOnlyBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy") };
        enemyOnly.SetBehavior<TagsBehavior, TagsBehavior>(
            in enemyOnlyBehavior,
            (ref readonly input, ref b) => b = input
        );
        
        var bossOnlyBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("boss") };
        bossOnly.SetBehavior<TagsBehavior, TagsBehavior>(
            in bossOnlyBehavior,
            (ref readonly input, ref b) => b = input
        );
        
        var enemyBossBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy", "boss") };
        enemyBoss.SetBehavior<TagsBehavior, TagsBehavior>(
            in enemyBossBehavior,
            (ref readonly input, ref b) => b = input
        );

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("#enemy:#boss", out var selector));
        selector.Recalc();

        // Assert - Only entity with BOTH tags matches
        Assert.False(selector.Matches.HasValue(enemyOnly.Index));
        Assert.False(selector.Matches.HasValue(bossOnly.Index));
        Assert.True(selector.Matches.HasValue(enemyBoss.Index));
    }

    [Fact]
    public void TagEntitySelector_RefinementWithIdAndTag_MatchesIntersection()
    {
        // Arrange
        var entity1 = EntityRegistry.Instance.Activate();
        var entity2 = EntityRegistry.Instance.Activate();

        var idBehavior1 = new IdBehavior("player");
        entity1.SetBehavior<IdBehavior, IdBehavior>(
            in idBehavior1,
            (ref readonly input, ref b) => b = input
        );
        var entity1Behavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy")  };
        entity1.SetBehavior<TagsBehavior, TagsBehavior>(
            in entity1Behavior,
            (ref readonly input, ref b) => b = input
        );

        var idBehavior2 = new IdBehavior("npc");
        entity2.SetBehavior<IdBehavior, IdBehavior>(
            in idBehavior2,
            (ref readonly input, ref b) => b = input
        );
        var entity2Behavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy")  };
        entity2.SetBehavior<TagsBehavior, TagsBehavior>(
            in entity2Behavior,
            (ref readonly input, ref b) => b = input
        );

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("@player:#enemy", out var selector));
        selector.Recalc();

        // Assert - Only entity with id "player" AND tag "enemy" matches
        Assert.True(selector.Matches.HasValue(entity1.Index));
        Assert.False(selector.Matches.HasValue(entity2.Index));
    }

    #endregion

    #region Selector Union

    [Fact]
    public void TagEntitySelector_Union_MatchesEntitiesWithEitherTag()
    {
        // Arrange
        var enemyOnly = EntityRegistry.Instance.Activate();
        var bossOnly = EntityRegistry.Instance.Activate();
        var neither = EntityRegistry.Instance.Activate();

        var enemyOnlyBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy")  };
        enemyOnly.SetBehavior<TagsBehavior, TagsBehavior>(
            in enemyOnlyBehavior,
            (ref readonly input, ref b) => b = input
        );
        var bossOnlyBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("boss")  };
        bossOnly.SetBehavior<TagsBehavior, TagsBehavior>(
            in bossOnlyBehavior,
            (ref readonly input, ref b) => b = input
        );
        var neitherBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("friendly")  };
        neither.SetBehavior<TagsBehavior, TagsBehavior>(
            in neitherBehavior,
            (ref readonly input, ref b) => b = input
        );

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("#enemy,#boss", out var selector));
        selector.Recalc();

        // Assert - Entities with "enemy" OR "boss" match
        Assert.True(selector.Matches.HasValue(enemyOnly.Index));
        Assert.True(selector.Matches.HasValue(bossOnly.Index));
        Assert.False(selector.Matches.HasValue(neither.Index));
    }

    [Fact]
    public void TagEntitySelector_UnionOfThreeTags_MatchesAny()
    {
        // Arrange
        var tag1 = EntityRegistry.Instance.Activate();
        var tag2 = EntityRegistry.Instance.Activate();
        var tag3 = EntityRegistry.Instance.Activate();
        var noTags = EntityRegistry.Instance.Activate();

        var tag1Behavior = new TagsBehavior { Tags = ImmutableHashSet.Create("tag1")  };
        tag1.SetBehavior<TagsBehavior, TagsBehavior>(
            in tag1Behavior,
            (ref readonly input, ref b) => b = input
        );
        var tag2Behavior = new TagsBehavior { Tags = ImmutableHashSet.Create("tag2")  };
        tag2.SetBehavior<TagsBehavior, TagsBehavior>(
            in tag2Behavior,
            (ref readonly input, ref b) => b = input
        );
        var tag3Behavior = new TagsBehavior { Tags = ImmutableHashSet.Create("tag3")  };
        tag3.SetBehavior<TagsBehavior, TagsBehavior>(
            in tag3Behavior,
            (ref readonly input, ref b) => b = input
        );

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("#tag1,#tag2,#tag3", out var selector));
        selector.Recalc();

        // Assert
        Assert.True(selector.Matches.HasValue(tag1.Index));
        Assert.True(selector.Matches.HasValue(tag2.Index));
        Assert.True(selector.Matches.HasValue(tag3.Index));
        Assert.False(selector.Matches.HasValue(noTags.Index));
    }

    #endregion

    #region Dirty Tracking

    [Fact]
    public void TagEntitySelector_AddTag_RecalculatesAndMatches()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        
        Assert.True(SelectorRegistry.Instance.TryParse("#enemy", out var selector));
        selector.Recalc();
        
        // Initially no match
        Assert.False(selector.Matches.HasValue(entity.Index));

        // Act - Add tag
        var entityBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy")  };
        entity.SetBehavior<TagsBehavior, TagsBehavior>(
            in entityBehavior,
            (ref readonly input, ref b) => b = input
        );
        selector.Recalc();

        // Assert - Now matches
        Assert.True(selector.Matches.HasValue(entity.Index));
    }

    [Fact]
    public void TagEntitySelector_RemoveTag_RecalculatesAndNoLongerMatches()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        var entityBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy")  };
        entity.SetBehavior<TagsBehavior, TagsBehavior>(
            in entityBehavior,
            (ref readonly input, ref b) => b = input
        );

        Assert.True(SelectorRegistry.Instance.TryParse("#enemy", out var selector));
        selector.Recalc();
        Assert.True(selector.Matches.HasValue(entity.Index));

        // Act - Remove behavior
        BehaviorRegistry<TagsBehavior>.Instance.Remove(entity);
        selector.Recalc();

        // Assert - No longer matches
        Assert.False(selector.Matches.HasValue(entity.Index));
    }

    [Fact]
    public void TagEntitySelector_UpdateTags_RecalculatesCorrectly()
    {
        // Arrange
        var entity = EntityRegistry.Instance.Activate();
        var entityBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("enemy")  };
        entity.SetBehavior<TagsBehavior, TagsBehavior>(
            in entityBehavior,
            (ref readonly input, ref b) => b = input
        );

        Assert.True(SelectorRegistry.Instance.TryParse("#enemy", out var enemySelector));
        Assert.True(SelectorRegistry.Instance.TryParse("#ally", out var allySelector));
        
        enemySelector.Recalc();
        allySelector.Recalc();

        Assert.True(enemySelector.Matches.HasValue(entity.Index));
        Assert.False(allySelector.Matches.HasValue(entity.Index));

        // Act - Swap tags
        var updatedBehavior = new TagsBehavior { Tags = ImmutableHashSet.Create("ally")  };
        entity.SetBehavior<TagsBehavior, TagsBehavior>(
            in updatedBehavior,
            (ref readonly input, ref b) => b = input
        );
        enemySelector.Recalc();
        allySelector.Recalc();

        // Assert
        Assert.False(enemySelector.Matches.HasValue(entity.Index));
        Assert.True(allySelector.Matches.HasValue(entity.Index));
    }

    #endregion

    #region Error Handling

    [Fact]
    public void TagEntitySelector_EmptyTagSelector_FiresErrorEvent()
    {
        // Arrange
        var errorListener = new FakeEventListener<ErrorEvent>();

        // Act
        var success = SelectorRegistry.Instance.TryParse("#", out _);

        // Assert
        Assert.False(success);
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void TagEntitySelector_TagThatDoesntExist_ValidButMatchesNothing()
    {
        // Arrange - No entities with "nonexistent" tag

        // Act
        Assert.True(SelectorRegistry.Instance.TryParse("#nonexistent", out var selector));
        selector.Recalc();

        // Assert - Valid selector but no matches
        var hasAnyMatches = false;
        foreach (var (_, _) in selector.Matches)
        {
            hasAnyMatches = true;
            break;
        }
        Assert.False(hasAnyMatches);
    }

    #endregion
}
