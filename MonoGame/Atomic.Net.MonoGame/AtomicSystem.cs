using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Sprites;
using Atomic.Net.MonoGame.Flex;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Persistence;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame;

public static class AtomicSystem
{
    public static void Initialize()
    {
        // Flex Behaviors
        BehaviorRegistry<AlignItemsBehavior>.Initialize();
        BehaviorRegistry<AlignSelfBehavior>.Initialize();
        BehaviorRegistry<BorderBottomBehavior>.Initialize();
        BehaviorRegistry<BorderLeftBehavior>.Initialize();
        BehaviorRegistry<BorderRightBehavior>.Initialize();
        BehaviorRegistry<BorderTopBehavior>.Initialize();
        BehaviorRegistry<FlexBehavior>.Initialize();
        BehaviorRegistry<FlexDirectionBehavior>.Initialize();
        BehaviorRegistry<FlexGrowBehavior>.Initialize();
        BehaviorRegistry<FlexWrapBehavior>.Initialize();
        BehaviorRegistry<FlexZOverride>.Initialize();
        BehaviorRegistry<HeightBehavior>.Initialize();
        BehaviorRegistry<JustifyContentBehavior>.Initialize();
        BehaviorRegistry<MarginBottomBehavior>.Initialize();
        BehaviorRegistry<MarginLeftBehavior>.Initialize();
        BehaviorRegistry<MarginRightBehavior>.Initialize();
        BehaviorRegistry<MarginTopBehavior>.Initialize();
        BehaviorRegistry<PaddingBottomBehavior>.Initialize();
        BehaviorRegistry<PaddingLeftBehavior>.Initialize();
        BehaviorRegistry<PaddingRightBehavior>.Initialize();
        BehaviorRegistry<PaddingTopBehavior>.Initialize();
        BehaviorRegistry<PositionBottomBehavior>.Initialize();
        BehaviorRegistry<PositionLeftBehavior>.Initialize();
        BehaviorRegistry<PositionRightBehavior>.Initialize();
        BehaviorRegistry<PositionTopBehavior>.Initialize();
        BehaviorRegistry<PositionTypeBehavior>.Initialize();
        BehaviorRegistry<WidthBehavior>.Initialize();

        // Hierarchy Behaviors
        BehaviorRegistry<ParentBehavior>.Initialize();

        // Id Behaviors
        BehaviorRegistry<IdBehavior>.Initialize();

        // Persistence Behaviors
        BehaviorRegistry<PersistToDiskBehavior>.Initialize();
        
        // Properties Behaviors
        BehaviorRegistry<PropertiesBehavior>.Initialize();

        // Sprite Behaviors
        BehaviorRegistry<DrawableBehavior>.Initialize();
        BehaviorRegistry<NineSliceBehavior>.Initialize();
        BehaviorRegistry<ThreeSliceBehavior>.Initialize();
        
        // Transform Behaviors
        BehaviorRegistry<TransformBehavior>.Initialize();
        BehaviorRegistry<WorldTransformBehavior>.Initialize();

        // Registries
        EntityRegistry.Initialize();
        FlexRegistry.Initialize();
        HierarchyRegistry.Initialize();
        EntityIdRegistry.Initialize();
        DatabaseRegistry.Initialize();
        PropertiesRegistry.Initialize();
        SelectorRegistry.Initialize();
        TransformRegistry.Initialize();
        // TODO : @senior-dev - enable
        // RuleRegistry.Initialize();

        // Scene System
        SceneLoader.Initialize();
    }
}
