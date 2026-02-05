using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Tries to activate the next available scene rule.
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <param name="index">The partition index of the activated rule, or null on error.</param>
    /// <returns>True if rule was activated successfully, false if capacity exceeded.</returns>
    public bool TryActivate(JsonRule rule, [NotNullWhen(true)] out PartitionIndex? index)
    {
        // Allocate from scene partition
        if (_nextSceneRuleIndex >= Constants.MaxSceneRules)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Scene rule capacity exceeded"));
            index = null;
            return false;
        }

        var sceneIdx = _nextSceneRuleIndex++;
        index = sceneIdx;
        Rules.Set(index.Value, rule);
        return true;
    }

    /// <summary>
    /// Tries to activate the next available global rule.
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <param name="index">The partition index of the activated rule, or null on error.</param>
    /// <returns>True if rule was activated successfully, false if capacity exceeded.</returns>
    public bool TryActivateGlobal(JsonRule rule, [NotNullWhen(true)] out PartitionIndex? index)
    {
        // Allocate from global partition
        if (_nextGlobalRuleIndex >= Constants.MaxGlobalRules)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Global rule capacity exceeded"));
            index = null;
            return false;
        }

        var globalIdx = _nextGlobalRuleIndex++;
        index = globalIdx;
        Rules.Set(index.Value, rule);
        return true;
    }

    /// <summary>
    /// Resets scene partition by clearing only scene rules.
    /// Called by ResetDriver during scene transitions.
    /// </summary>
    public void Reset()
    {
        // senior-dev: Clear scene partition with O(1) operation
        Rules.Scene.Clear();

        // senior-dev: Reset scene index allocator
        _nextSceneRuleIndex = 0;
    }

    /// <summary>
    /// Handle reset event by deactivating only scene rules.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        Reset();
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
