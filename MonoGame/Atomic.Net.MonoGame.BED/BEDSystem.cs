using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.BED.Persistence;

namespace Atomic.Net.MonoGame.BED;
public static class BEDSystem
{
    public static void Initialize()
    {
        BehaviorRegistry<Parent>.Initialize();
        BehaviorRegistry<IdBehavior>.Initialize();
        BehaviorRegistry<PropertiesBehavior>.Initialize();
        BehaviorRegistry<PersistToDiskBehavior>.Initialize();

        // senior-dev: EntityIdRegistry is a core behavior registry, must initialize here
        EntityIdRegistry.Initialize();
        HierarchyRegistry.Initialize();
        PropertyBagIndex.Initialize();
    }
}
