using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Singleton registry for tracking entity IDs and resolving parent references.
/// Maintains a dictionary mapping string IDs to entities.
/// </summary>
public sealed class EntityIdRegistry : ISingleton<EntityIdRegistry>,
    IEventHandler<BehaviorAddedEvent<EntityId>>,
    IEventHandler<PreEntityDeactivatedEvent>,
    IEventHandler<ResetEvent>
{
    private static EntityIdRegistry? _instance;
    public static EntityIdRegistry Instance => _instance ??= new EntityIdRegistry();

    private readonly Dictionary<string, Entity> _idToEntity = new();

    private EntityIdRegistry()
    {
        EventBus<BehaviorAddedEvent<EntityId>>.Register(this);
        EventBus<PreEntityDeactivatedEvent>.Register(this);
        EventBus<ResetEvent>.Register(this);
    }

    /// <summary>
    /// Attempts to register an entity with the given ID.
    /// Returns true if registered successfully (first-write-wins).
    /// Returns false if the ID is already registered.
    /// </summary>
    public bool TryRegister(Entity entity, string id)
    {
        // test-architect: Stub - To be implemented by @senior-dev
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    /// <summary>
    /// Attempts to resolve an entity by its ID.
    /// Returns true and outputs the entity if found, false otherwise.
    /// </summary>
    public bool TryResolve(string id, out Entity entity)
    {
        // test-architect: Stub - To be implemented by @senior-dev
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void OnEvent(BehaviorAddedEvent<EntityId> e)
    {
        // test-architect: Stub - To be implemented by @senior-dev
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        // test-architect: Stub - To be implemented by @senior-dev
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void OnEvent(ResetEvent e)
    {
        // test-architect: Stub - To be implemented by @senior-dev
        throw new NotImplementedException("To be implemented by @senior-dev");
    }
}
