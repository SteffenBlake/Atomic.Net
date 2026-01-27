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
    private readonly SparseArray<bool> _active = new(Constants.MaxRules);

    /// <summary>
    /// Public accessor for rules SparseArray for iteration and testing.
    /// </summary>
    public SparseArray<JsonRule> Rules => _rules;

    /// <summary>
    /// Activate the next available scene rule (index greater than or equal to MaxGlobalRules).
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <returns>The index of the activated rule.</returns>
    public ushort Activate(JsonRule rule)
    {
        // test-architect: Stub implementation - to be implemented by @senior-dev
        throw new NotImplementedException("RuleRegistry.Activate() - Scene partition rule activation not yet implemented");
    }

    /// <summary>
    /// Activate the next available global rule (index less than MaxGlobalRules).
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <returns>The index of the activated rule.</returns>
    public ushort ActivateGlobal(JsonRule rule)
    {
        // test-architect: Stub implementation - to be implemented by @senior-dev
        throw new NotImplementedException("RuleRegistry.ActivateGlobal() - Global partition rule activation not yet implemented");
    }

    /// <summary>
    /// Handle reset event by deactivating only scene rules.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // test-architect: Stub implementation - to be implemented by @senior-dev
        throw new NotImplementedException("RuleRegistry.OnEvent(ResetEvent) - Scene rule cleanup not yet implemented");
    }

    /// <summary>
    /// Handle shutdown event by deactivating ALL rules (both global and scene).
    /// Used for complete game shutdown and test cleanup.
    /// </summary>
    public void OnEvent(ShutdownEvent _)
    {
        // test-architect: Stub implementation - to be implemented by @senior-dev
        throw new NotImplementedException("RuleRegistry.OnEvent(ShutdownEvent) - Full rule cleanup not yet implemented");
    }
}
