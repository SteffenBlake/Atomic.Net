using Atomic.Net.MonoGame.BED.Hierarchy;

namespace Atomic.Net.MonoGame.BED;
public static class BEDSystem
{
    public static void Initialize()
    {
        BehaviorRegistry<Parent>.Initialize();
        BehaviorRegistry<IdBehavior>.Initialize();

        HierarchyRegistry.Initialize();
    }
}
