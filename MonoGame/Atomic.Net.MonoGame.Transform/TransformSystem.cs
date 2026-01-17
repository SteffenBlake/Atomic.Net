using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Transform;

public static class TransformSystem
{
    public static void Initialize()
    {
        BehaviorRegistry<TransformBehavior>.Initialize();
        BehaviorRegistry<WorldTransformBehavior>.Initialize();
        
        // CRITICAL: Must initialize RefBehaviorRegistry to register for PreEntityDeactivatedEvent
        // Without this, TransformBehavior won't be cleaned up when entities are deactivated!
        _ = RefBehaviorRegistry<TransformBehavior>.Instance;

        TransformBackingStore.Initialize();
        ParentWorldTransformBackingStore.Initialize();
        WorldTransformBackingStore.Initialize();
        
        LocalTransformBlockMapSet.Initialize();
        WorldTransformBlockMapSet.Initialize();

        TransformRegistry.Initialize();
    }
}
