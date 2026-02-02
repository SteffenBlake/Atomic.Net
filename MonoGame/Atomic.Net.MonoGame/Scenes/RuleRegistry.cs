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

    public readonly PartitionedSparseArray<JsonRule> Rules = new(
        Constants.MaxGlobalRules,
        Constants.MaxSceneRules
    );
    private uint _nextSceneRuleIndex = 0;
    private ushort _nextGlobalRuleIndex = 0;

    // @senior-dev: Change this to:
    // bool TryActivate(JsonRule rule, [NotNullWhen(true)] out PartitionIndex? index) ...

    /// <summary>
    /// Activate the next available scene rule.
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <returns>The partition index of the activated rule, or null on error.</returns>
    public PartitionIndex? Activate(JsonRule rule)
    {
        // senior-dev: Allocate from scene partition
        if (_nextSceneRuleIndex >= Constants.MaxSceneRules)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Scene rule capacity exceeded"));
            return null;
        }

        var index = _nextSceneRuleIndex++;
        PartitionIndex sceneIndex = index;
        Rules.Set(sceneIndex, rule);
        return sceneIndex;
    }

    /// <summary>
    /// Activate the next available global rule.
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <returns>The partition index of the activated rule, or null on error.</returns>
    public PartitionIndex? ActivateGlobal(JsonRule rule)
    {
        // senior-dev: Allocate from global partition
        if (_nextGlobalRuleIndex >= Constants.MaxGlobalRules)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Global rule capacity exceeded"));
            return null;
        }

        var index = _nextGlobalRuleIndex++;
        PartitionIndex globalIndex = index;
        Rules.Set(globalIndex, rule);
        return globalIndex;
    }

    /// <summary>
    /// Handle reset event by deactivating only scene rules.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // senior-dev: Clear scene partition with O(1) operation
        Rules.Scene.Clear();

        // senior-dev: Reset scene index allocator
        _nextSceneRuleIndex = 0;
    }

    /// <summary>
    /// Handle shutdown event by deactivating ALL rules (both global and scene).
    /// Used for complete game shutdown and test cleanup.
    /// </summary>
    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Clear all partitions
        Rules.Global.Clear();
        Rules.Scene.Clear();

        // senior-dev: Reset both index allocators
        _nextGlobalRuleIndex = 0;
        _nextSceneRuleIndex = 0;
    }
}
