using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.BED.Properties;

namespace Atomic.Net.MonoGame.BED;
public static class BEDSystem
{
    public static void Initialize()
    {
        BehaviorRegistry<Parent>.Initialize();
        BehaviorRegistry<IdBehavior>.Initialize();
        BehaviorRegistry<PropertiesBehavior>.Initialize();

        EntityIdRegistry.Initialize();
        HierarchyRegistry.Initialize();
        PropertyBagIndex.Initialize();
    }
}
