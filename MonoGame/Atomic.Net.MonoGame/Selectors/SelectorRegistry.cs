using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Tags;

namespace Atomic.Net.MonoGame.Selectors;

public class SelectorRegistry :
    ISingleton<SelectorRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<ShutdownEvent>,
    IEventHandler<BehaviorAddedEvent<IdBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<IdBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<IdBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<IdBehavior>>,
    IEventHandler<BehaviorAddedEvent<TagsBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<TagsBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<TagsBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<TagsBehavior>>
{
    public static SelectorRegistry Instance { get; private set; } = null!;

    public static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    // Lookups for fast locating "full" selector by their Prior+Self hashcode 
    // Sized for both partitions combined, divided by typical selector reuse factor
    private readonly Dictionary<int, UnionEntitySelector> _unionSelectorRegistry = new((Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities) / 64);
    private readonly Dictionary<int, IdEntitySelector> _idSelectorRegistry = new((Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities) / 64);
    private readonly Dictionary<int, TagEntitySelector> _tagSelectorRegistry = new((Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities) / 64);
    private readonly Dictionary<int, CollisionEnterEntitySelector> _enterSelectorRegistry = new((Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities) / 64);
    private readonly Dictionary<int, CollisionExitEntitySelector> _exitSelectorRegistry = new((Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities) / 64);

    // Lookup for @Id => IdEntitySelector (for tagging dirty)
    private readonly Dictionary<string, IdEntitySelector> _idSelectorLookup = new((Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities) / 64);

    // Lookup for @Tag => IdEntitySelector (for tagging dirty)
    private readonly Dictionary<string, TagEntitySelector> _tagSelectorLookup = new((Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities) / 64);

    // Track root selectors (final parsed results) for bulk Recalc
    // senior-dev: Only track the final "root" nodes from TryParse to avoid recalculating
    // the same sub-nodes multiple times. Each root recursively recalcs its children.
    private readonly HashSet<EntitySelector> _rootSelectors = new((Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities) / 64);

    // senior-dev: Pre-allocated buffer for union parts to achieve zero allocations during parsing
    private readonly List<EntitySelector> _unionPartsBuffer = new(32);

    /// <summary>
    /// Recalculates all root selectors that have been parsed.
    /// Call this after scene loading or when entities/IDs change to update selector matches.
    /// </summary>
    public void Recalc()
    {
        foreach (var selector in _rootSelectors)
        {
            selector.Recalc();
        }
    }

    public bool TryParse(
        ReadOnlySpan<char> tokens,
        [NotNullWhen(true)]
        out EntitySelector? entitySelector
    )
    {
        entitySelector = null;

        if (tokens.IsEmpty)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Empty selector string"));
            return false;
        }

        // senior-dev: Clear buffer at start to ensure clean state (zero allocations)
        _unionPartsBuffer.Clear();

        EntitySelector? current = null;
        var segmentStart = 0;

        for (var i = 0; i <= tokens.Length; i++)
        {
            var c = i < tokens.Length ? tokens[i] : '\0';

            // Process delimiters and end of input
            if (c is ':' or ',' or '\0')
            {
                if (!TryProcessToken(tokens, segmentStart, i, current, out current))
                {
                    return false;
                }

                // Add to union only on comma or end (not on colon)
                if (c is ',' or '\0')
                {
                    if (current != null)
                    {
                        _unionPartsBuffer.Add(current);
                    }
                    current = null;
                }

                segmentStart = i + 1;
                continue;
            }

            // Validate character
            if (IsValidCharacter(c, i == segmentStart))
            {
                continue;
            }

            EventBus<ErrorEvent>.Push(
                new ErrorEvent($"Invalid character '{c}' at position {i}")
            );
            return false;
        }

        // Build final result
        if (_unionPartsBuffer.Count == 0)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("No valid selectors parsed"));
            return false;
        }

        entitySelector = _unionPartsBuffer.Count == 1
            ? _unionPartsBuffer[0]
            : GetOrCreateUnionSelector(tokens, _unionPartsBuffer);

        // senior-dev: Track this as a root selector for bulk Recalc
        _rootSelectors.Add(entitySelector);

        // senior-dev: Clear buffer at end for next parse (zero allocations)
        _unionPartsBuffer.Clear();

        return true;
    }

    private bool TryProcessToken(
        ReadOnlySpan<char> tokens,
        int segmentStart,
        int segmentEnd,
        EntitySelector? prior,
        out EntitySelector? result
    )
    {
        result = null;

        // Guard: empty token
        if (segmentEnd == segmentStart)
        {
            EventBus<ErrorEvent>.Push(
                new ErrorEvent($"Empty token at position {segmentEnd}")
            );
            return false;
        }

        var fullPath = tokens[0..segmentEnd];
        var token = tokens[segmentStart..segmentEnd];
        var hash = string.GetHashCode(fullPath);

        if (!TryGetOrCreateSelector(token, hash, prior, out var selector))
        {
            return false;
        }

        result = selector;
        return true;
    }

    private EntitySelector GetOrCreateUnionSelector(
        ReadOnlySpan<char> tokens,
        List<EntitySelector> children)
    {
        var hash = string.GetHashCode(tokens);

        if (_unionSelectorRegistry.TryGetValue(hash, out var cached))
        {
            // Union recalcs when children recalc, and children are already marked dirty
            // via TryGetOrCreate* methods, so no need to mark union dirty
            return cached;
        }

        // senior-dev: Create a new List copy since UnionEntitySelector stores the reference
        // and we reuse _unionPartsBuffer for zero allocations
        var childrenCopy = new List<EntitySelector>(children);
        var union = new UnionEntitySelector(hash, childrenCopy);
        _unionSelectorRegistry[hash] = union;
        return union;
    }

    private static bool IsValidCharacter(char c, bool isFirstInToken)
    {
        // Prefix characters only valid at token start
        if (isFirstInToken && c is '@' or '#' or '!')
        {
            return true;
        }

        // Valid identifier characters
        return c is (>= 'a' and <= 'z') or
                    (>= 'A' and <= 'Z') or
                    (>= '0' and <= '9') or
                    '_' or '-';
    }

    private bool TryGetOrCreateSelector(
        ReadOnlySpan<char> token,
        int hash,
        EntitySelector? prior,
        [NotNullWhen(true)] out EntitySelector? selector
    )
    {
        selector = null;

        // Guard: empty token
        if (token.IsEmpty)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Empty token"));
            return false;
        }

        return token[0] switch
        {
            '@' => TryGetOrCreateIdSelector(token, hash, prior, out selector),
            '#' => TryGetOrCreateTagSelector(token, hash, prior, out selector),
            '!' => TryGetOrCreateCollisionSelector(token, hash, prior, out selector),
            _ => PushPrefixError(token[0])
        };
    }

    static bool PushPrefixError(char prefix)
    {
        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Invalid selector prefix '{prefix}'. Must be @, #, or !"));
        return false;
    }

    private bool TryGetOrCreateIdSelector(
        ReadOnlySpan<char> token,
        int hash,
        EntitySelector? prior,
        [NotNullWhen(true)] out EntitySelector? selector
    )
    {
        // Guard: cache hit
        if (_idSelectorRegistry.TryGetValue(hash, out var match))
        {
            selector = match;
            return true;
        }

        // Guard: validate token has identifier
        if (token.Length is 1)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("ID selector must have an identifier after @"));
            selector = null;
            return false;
        }

        // Create new selector
        var id = token[1..].ToString();
        var newSelector = new IdEntitySelector(hash, id, prior);

        _idSelectorRegistry[hash] = newSelector;
        _idSelectorLookup[id] = newSelector;

        selector = newSelector;
        return true;
    }

    private bool TryGetOrCreateTagSelector(
        ReadOnlySpan<char> token,
        int hash,
        EntitySelector? prior,
        [NotNullWhen(true)] out EntitySelector? selector
    )
    {
        // Guard: cache hit
        if (_tagSelectorRegistry.TryGetValue(hash, out var match))
        {
            selector = match;
            return true;
        }

        // Guard: validate token has identifier
        if (token.Length is 1)
        {
            EventBus<ErrorEvent>.Push(
                new ErrorEvent("Tag selector must have an identifier after #")
            );
            selector = null;
            return false;
        }

        // Create new selector
        var tag = token[1..].ToString();
        var newSelector = new TagEntitySelector(hash, tag, prior);

        _tagSelectorRegistry[hash] = newSelector;
        _tagSelectorLookup[tag] = newSelector;

        selector = newSelector;
        return true;
    }

    private bool TryGetOrCreateCollisionSelector(
        ReadOnlySpan<char> token,
        int hash,
        EntitySelector? prior,
        [NotNullWhen(true)] out EntitySelector? selector)
    {
        // Check for !enter
        if (token.SequenceEqual("!enter"))
        {
            // Guard: cache hit
            if (_enterSelectorRegistry.TryGetValue(hash, out var match))
            {
                selector = match;
                return true;
            }

            _enterSelectorRegistry[hash] = new CollisionEnterEntitySelector(hash, prior);
            selector = _enterSelectorRegistry[hash];

            return true;
        }

        // Check for !exit
        if (token.SequenceEqual("!exit"))
        {
            // Guard: cache hit
            if (_exitSelectorRegistry.TryGetValue(hash, out var match))
            {
                selector = match;
                return true;
            }

            _exitSelectorRegistry[hash] = new CollisionExitEntitySelector(hash, prior);
            selector = _exitSelectorRegistry[hash];  // senior-dev: Fixed copy-paste error

            return true;
        }

        // Invalid collision keyword
        EventBus<ErrorEvent>.Push(new ErrorEvent(
            "Invalid collision selector. Must be !enter or !exit"
        ));
        selector = null;
        return false;
    }

    public void OnEvent(InitializeEvent e)
    {
        EventBus<ShutdownEvent>.Register(Instance);
        EventBus<BehaviorAddedEvent<IdBehavior>>.Register(Instance);
        EventBus<PreBehaviorUpdatedEvent<IdBehavior>>.Register(Instance);
        EventBus<PostBehaviorUpdatedEvent<IdBehavior>>.Register(Instance);
        EventBus<PreBehaviorRemovedEvent<IdBehavior>>.Register(Instance);
        EventBus<BehaviorAddedEvent<TagsBehavior>>.Register(Instance);
        EventBus<PreBehaviorUpdatedEvent<TagsBehavior>>.Register(Instance);
        EventBus<PostBehaviorUpdatedEvent<TagsBehavior>>.Register(Instance);
        EventBus<PreBehaviorRemovedEvent<TagsBehavior>>.Register(Instance);
    }

    public void OnEvent(ShutdownEvent e)
    {
        _unionSelectorRegistry.Clear();
        _idSelectorRegistry.Clear();
        _tagSelectorRegistry.Clear();
        _enterSelectorRegistry.Clear();
        _exitSelectorRegistry.Clear();
        _idSelectorLookup.Clear();
        _tagSelectorLookup.Clear();
        _rootSelectors.Clear();
        _unionPartsBuffer.Clear();
    }

    /// <summary>
    /// Resets scene partition by recalculating all selectors.
    /// Called by ResetDriver during scene transitions.
    /// </summary>
    public void Reset()
    {
        // On Reset, recalc all selectors to update Matches for non-persistent entities
        // Scene entities (>= MaxGlobalEntities) are deactivated by EntityRegistry
        // Their IdBehaviors are removed, marking selectors dirty
        // We recalc here to update all selector Matches arrays
        Recalc();
    }

    public void OnEvent(BehaviorAddedEvent<IdBehavior> e)
    {
        OnIdBehaviorMutated(e.Entity);
    }

    public void OnEvent(PreBehaviorUpdatedEvent<IdBehavior> e)
    {
        OnIdBehaviorMutated(e.Entity);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<IdBehavior> e)
    {
        OnIdBehaviorMutated(e.Entity);
    }

    public void OnEvent(PreBehaviorRemovedEvent<IdBehavior> e)
    {
        OnIdBehaviorMutated(e.Entity);
    }

    private void OnIdBehaviorMutated(Entity entity)
    {
        if (entity.TryGetBehavior<IdBehavior>(out var behavior))
        {
            if (_idSelectorLookup.TryGetValue(behavior.Value.Id, out var selector))
            {
                selector.MarkDirty();
            }
        }
    }

    public void OnEvent(BehaviorAddedEvent<TagsBehavior> e)
    {
        MarkTagSelectorsAsDirty(e.Entity);
    }

    public void OnEvent(PreBehaviorUpdatedEvent<TagsBehavior> e)
    {
        MarkTagSelectorsAsDirty(e.Entity);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<TagsBehavior> e)
    {
        MarkTagSelectorsAsDirty(e.Entity);
    }

    public void OnEvent(PreBehaviorRemovedEvent<TagsBehavior> e)
    {
        MarkTagSelectorsAsDirty(e.Entity);
    }

    private void MarkTagSelectorsAsDirty(Entity entity)
    {
        // senior-dev: When tags change on an entity, mark all selectors for those tags as dirty
        if (entity.TryGetBehavior<TagsBehavior>(out var behavior))
        {
            foreach (var tag in behavior.Value.Tags)
            {
                if (_tagSelectorLookup.TryGetValue(tag, out var selector))
                {
                    selector.MarkDirty();
                }
            }
        }
    }
}
