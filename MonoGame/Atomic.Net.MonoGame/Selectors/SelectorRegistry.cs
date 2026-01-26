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
    IEventHandler<PreBehaviorRemovedEvent<IdBehavior>>
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

        EntitySelector? current = null;
        List<EntitySelector>? unionParts = null;
        var segmentStart = 0;

        for (var i = 0; i <= tokens.Length; i++)
        {
            var c = i < tokens.Length ? tokens[i] : '\0';

            // Process delimiters and end of input
            if (c is ':' or ',' or '\0')
            {
                if (!TryProcessToken(tokens, segmentStart, i, current, ref unionParts, out current))
                {
                    return false;
                }

                // Reset current for union segments
                if (c is ',' or '\0')
                {
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
        if (unionParts is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("No valid selectors parsed"));
            return false;
        }

        entitySelector = unionParts.Count == 1
            ? unionParts[0]
            : GetOrCreateUnionSelector(tokens, unionParts);

        return true;
    }

    private bool TryProcessToken(
        ReadOnlySpan<char> tokens,
        int segmentStart,
        int segmentEnd,
        EntitySelector? prior,
        ref List<EntitySelector>? unionParts,
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

        unionParts ??= [];
        unionParts.Add(selector);
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
            selector = _enterSelectorRegistry[hash];

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
