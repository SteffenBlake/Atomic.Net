using Atomic.Net.MonoGame.BED.Hierarchy;

namespace Atomic.Net.MonoGame.BED;
public static class BEDSystem
{
    public static void Initialize()
    {
        BehaviorRegistry<Parent>.Initialize();
        BehaviorRegistry<IdBehavior>.Initialize();

        // senior-dev: EntityIdRegistry is a core behavior registry, must initialize here
        EntityIdRegistry.Initialize();
        HierarchyRegistry.Initialize();
    }
}
