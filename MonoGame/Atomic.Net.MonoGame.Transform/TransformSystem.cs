using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Transform;

public static class TransformSystem
{
    public static void Initialize()
    {
        BehaviorRegistry<TransformBehavior>.Initialize();
        BehaviorRegistry<WorldTransformBehavior>.Initialize();
        RefBehaviorRegistry<TransformBehavior>.Initialize();
        RefBehaviorRegistry<WorldTransformBehavior>.Initialize();

        TransformBackingStore.Initialize();
        ParentWorldTransformBackingStore.Initialize();
        
        LocalTransformBlockMapSet.Initialize();
        WorldTransformBlockMapSet.Initialize(dense: false);

        TransformRegistry.Initialize();
    }
}
