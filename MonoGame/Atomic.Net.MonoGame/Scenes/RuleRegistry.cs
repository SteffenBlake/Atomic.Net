using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Central registry for rule lifecycle management.
/// Manages Global vs Scene partition allocation similar to EntityRegistry.
/// </summary>
public class RuleRegistry : IEventHandler<ResetEvent>, IEventHandler<ShutdownEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<ResetEvent>.Register(Instance);
        EventBus<ShutdownEvent>.Register(Instance);
    }

    public static RuleRegistry Instance { get; private set; } = null!;

    private readonly SparseArray<JsonRule> _rules = new(Constants.MaxRules);
    private ushort _nextSceneRuleIndex = Constants.MaxGlobalRules;
    private ushort _nextGlobalRuleIndex = 0;

    /// <summary>
    /// Public accessor for rules SparseArray for iteration and testing.
    /// </summary>
    public SparseArray<JsonRule> Rules => _rules;

    /// <summary>
    /// Activate the next available scene rule (index greater than or equal to MaxGlobalRules).
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <returns>The index of the activated rule, or ushort.MaxValue on error.</returns>
    public ushort Activate(JsonRule rule)
    {
        // senior-dev: Allocate from scene partition (>= MaxGlobalRules)
        if (_nextSceneRuleIndex >= Constants.MaxRules)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Scene rule capacity exceeded"));
            return ushort.MaxValue;
        }

        var index = _nextSceneRuleIndex++;
        _rules.Set(index, rule);
        return index;
    }

    /// <summary>
    /// Activate the next available global rule (index less than MaxGlobalRules).
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <returns>The index of the activated rule, or ushort.MaxValue on error.</returns>
    public ushort ActivateGlobal(JsonRule rule)
    {
        // senior-dev: Allocate from global partition (< MaxGlobalRules)
        if (_nextGlobalRuleIndex >= Constants.MaxGlobalRules)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Global rule capacity exceeded"));
            return ushort.MaxValue;
        }

        var index = _nextGlobalRuleIndex++;
        _rules.Set(index, rule);
        return index;
    }

    /// <summary>
    /// Handle reset event by deactivating only scene rules.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // senior-dev: Clear scene partition only (>= MaxGlobalRules)
        for (ushort i = Constants.MaxGlobalRules; i < Constants.MaxRules; i++)
        {
            if (_rules.HasValue(i))
            {
                _rules.Remove(i);
            }
        }

        // senior-dev: Reset scene index allocator
        _nextSceneRuleIndex = Constants.MaxGlobalRules;
    }

    /// <summary>
    /// Handle shutdown event by deactivating ALL rules (both global and scene).
    /// Used for complete game shutdown and test cleanup.
    /// </summary>
    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Clear all partitions (both global and scene)
        _rules.Clear();

        // senior-dev: Reset both index allocators
        _nextGlobalRuleIndex = 0;
        _nextSceneRuleIndex = Constants.MaxGlobalRules;
    }
}
