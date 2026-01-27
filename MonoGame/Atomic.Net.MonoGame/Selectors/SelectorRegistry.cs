using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;

namespace Atomic.Net.MonoGame.Selectors;

public class SelectorRegistry :
    ISingleton<SelectorRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<IdBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<IdBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<IdBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<IdBehavior>>,
    IEventHandler<ResetEvent>,
    IEventHandler<ShutdownEvent>
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
    private readonly Dictionary<int, UnionEntitySelector> _unionSelectorRegistry = new(Constants.MaxEntities / 64);
    private readonly Dictionary<int, IdEntitySelector> _idSelectorRegistry = new(Constants.MaxEntities / 64);
    private readonly Dictionary<int, TagEntitySelector> _tagSelectorRegistry = new(Constants.MaxEntities / 64);
    private readonly Dictionary<int, CollisionEnterEntitySelector> _enterSelectorRegistry = new(Constants.MaxEntities / 64);
    private readonly Dictionary<int, CollisionExitEntitySelector> _exitSelectorRegistry = new(Constants.MaxEntities / 64);

    // Lookup for @Id => IdEntitySelector (for tagging dirty)
    private readonly Dictionary<string, IdEntitySelector> _idSelectorLookup = new(Constants.MaxEntities / 64);

    // Lookup for @Tag => IdEntitySelector (for tagging dirty)
    private readonly Dictionary<string, TagEntitySelector> _tagSelectorLookup = new(Constants.MaxEntities / 64);

    // Track root selectors (final parsed results) for bulk Recalc
    // senior-dev: Only track the final "root" nodes from TryParse to avoid recalculating
    // the same sub-nodes multiple times. Each root recursively recalcs its children.
    private readonly HashSet<EntitySelector> _rootSelectors = new(Constants.MaxEntities / 64);

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

        List<EntitySelector>? unionParts = null;
        List<(int start, int end)>? chainTokens = null;
        var segmentStart = 0;

        // senior-dev: First pass - collect token positions for each refinement chain
        for (var i = 0; i <= tokens.Length; i++)
        {
            var c = i < tokens.Length ? tokens[i] : '\0';

            // Process delimiters and end of input
            if (c is ':' or ',' or '\0')
            {
                if (i > segmentStart)
                {
                    chainTokens ??= [];
                    chainTokens.Add((segmentStart, i));
                }

                // senior-dev: On comma or end, build the refinement chain and add to union
                if (c is ',' or '\0')
                {
                    if (chainTokens != null && chainTokens.Count > 0)
                    {
                        if (!TryBuildRefinementChain(tokens, chainTokens, out var chain))
                        {
                            return false;
                        }
                        
                        unionParts ??= [];
                        unionParts.Add(chain);
                        chainTokens.Clear();
                    }
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
        if (unionParts is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("No valid selectors parsed"));
            return false;
        }

        entitySelector = unionParts.Count == 1
            ? unionParts[0]
            : GetOrCreateUnionSelector(tokens, unionParts);

        // senior-dev: Track this as a root selector for bulk Recalc
        _rootSelectors.Add(entitySelector);

        return true;
    }

    // senior-dev: Build refinement chain from RIGHT to LEFT per sprint requirements
    // "!enter:#enemies" → CollisionEnter(Prior: Tagged("enemies"))
    // Parse #enemies first (rightmost), then !enter with #enemies as its prior
    private bool TryBuildRefinementChain(
        ReadOnlySpan<char> tokens,
        List<(int start, int end)> chainTokens,
        [NotNullWhen(true)] out EntitySelector? result
    )
    {
        result = null;
        EntitySelector? prior = null;

        // senior-dev: Build from right to left
        for (var i = chainTokens.Count - 1; i >= 0; i--)
        {
            var (start, end) = chainTokens[i];
            var fullPath = tokens[0..end];
            var token = tokens[start..end];
            var hash = string.GetHashCode(fullPath);

            if (!TryGetOrCreateSelector(token, hash, prior, out var selector))
            {
                return false;
            }

            prior = selector;
        }

        result = prior;
        return true;
    }

    private EntitySelector GetOrCreateUnionSelector(
        ReadOnlySpan<char> tokens,
        List<EntitySelector> children)
    {
        var hash = string.GetHashCode(tokens);

        if (_unionSelectorRegistry.TryGetValue(hash, out var cached))
        {
            return cached;
        }

        var union = new UnionEntitySelector(hash, children);
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
        EventBus<BehaviorAddedEvent<IdBehavior>>.Register(Instance);
        EventBus<PreBehaviorUpdatedEvent<IdBehavior>>.Register(Instance);
        EventBus<PostBehaviorUpdatedEvent<IdBehavior>>.Register(Instance);
        EventBus<PreBehaviorRemovedEvent<IdBehavior>>.Register(Instance);
        EventBus<ResetEvent>.Register(Instance);
        EventBus<ShutdownEvent>.Register(Instance);
    }

    public void OnEvent(ResetEvent _)
    {
        // senior-dev: On Reset, recalc all selectors to update Matches for non-persistent entities
        // Scene entities (>= MaxGlobalEntities) are deactivated by EntityRegistry
        // Their IdBehaviors are removed, marking selectors dirty
        // We recalc here to update all selector Matches arrays
        Recalc();
    }

    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Shutdown clears EVERYTHING (used between tests)
        _unionSelectorRegistry.Clear();
        _idSelectorRegistry.Clear();
        _tagSelectorRegistry.Clear();
        _enterSelectorRegistry.Clear();
        _exitSelectorRegistry.Clear();
        _idSelectorLookup.Clear();
        _tagSelectorLookup.Clear();
        _rootSelectors.Clear();
        
        // Unregister from all events to prevent duplicate registrations
        EventBus<BehaviorAddedEvent<IdBehavior>>.Unregister(Instance);
        EventBus<PreBehaviorUpdatedEvent<IdBehavior>>.Unregister(Instance);
        EventBus<PostBehaviorUpdatedEvent<IdBehavior>>.Unregister(Instance);
        EventBus<PreBehaviorRemovedEvent<IdBehavior>>.Unregister(Instance);
        EventBus<ResetEvent>.Unregister(Instance);
        EventBus<ShutdownEvent>.Unregister(Instance);
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
}
