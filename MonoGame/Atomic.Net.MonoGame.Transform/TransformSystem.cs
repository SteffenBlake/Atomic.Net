using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Transform;

public static class TransformSystem
{
    public static void Initialize()
    {
        RefBehaviorRegistry<TransformBehavior>.Initialize();
        RefBehaviorRegistry<WorldTransformBehavior>.Initialize();

        TransformStore.Initialize();
        WorldTransformStore.Initialize();
        ParentWorldTransformStore.Initialize();

        TransformRegistry.Initialize();
    }
}
