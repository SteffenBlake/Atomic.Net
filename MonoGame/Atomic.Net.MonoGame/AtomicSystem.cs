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
using Atomic.Net.MonoGame.Sequencing;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame;

public static class AtomicSystem
{
    public static void Initialize()
    {
        // Flex Behaviors
        BehaviorRegistry<FlexAlignItemsBehavior>.Initialize();
        BehaviorRegistry<FlexAlignSelfBehavior>.Initialize();
        BehaviorRegistry<FlexBorderBottomBehavior>.Initialize();
        BehaviorRegistry<FlexBorderLeftBehavior>.Initialize();
        BehaviorRegistry<FlexBorderRightBehavior>.Initialize();
        BehaviorRegistry<FlexBorderTopBehavior>.Initialize();
        BehaviorRegistry<FlexBehavior>.Initialize();
        BehaviorRegistry<FlexDirectionBehavior>.Initialize();
        BehaviorRegistry<FlexGrowBehavior>.Initialize();
        BehaviorRegistry<FlexWrapBehavior>.Initialize();
        BehaviorRegistry<FlexZOverride>.Initialize();
        BehaviorRegistry<FlexHeightBehavior>.Initialize();
        BehaviorRegistry<FlexJustifyContentBehavior>.Initialize();
        BehaviorRegistry<FlexMarginBottomBehavior>.Initialize();
        BehaviorRegistry<FlexMarginLeftBehavior>.Initialize();
        BehaviorRegistry<FlexMarginRightBehavior>.Initialize();
        BehaviorRegistry<FlexMarginTopBehavior>.Initialize();
        BehaviorRegistry<FlexPaddingBottomBehavior>.Initialize();
        BehaviorRegistry<FlexPaddingLeftBehavior>.Initialize();
        BehaviorRegistry<FlexPaddingRightBehavior>.Initialize();
        BehaviorRegistry<FlexPaddingTopBehavior>.Initialize();
        BehaviorRegistry<FlexPositionBottomBehavior>.Initialize();
        BehaviorRegistry<FlexPositionLeftBehavior>.Initialize();
        BehaviorRegistry<FlexPositionRightBehavior>.Initialize();
        BehaviorRegistry<FlexPositionTopBehavior>.Initialize();
        BehaviorRegistry<FlexPositionTypeBehavior>.Initialize();
        BehaviorRegistry<FlexWidthBehavior>.Initialize();

        // Hierarchy Behaviors
        BehaviorRegistry<ParentBehavior>.Initialize();

        // Id Behaviors
        BehaviorRegistry<IdBehavior>.Initialize();

        // Tags Behaviors
        BehaviorRegistry<TagsBehavior>.Initialize();

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
        TagRegistry.Initialize();
        DatabaseRegistry.Initialize();
        PropertiesRegistry.Initialize();
        SelectorRegistry.Initialize();
        TransformRegistry.Initialize();
        RuleRegistry.Initialize();
        SequenceRegistry.Initialize();

        // Scene System
        SceneLoader.Initialize();
        SceneManager.Initialize();
        RulesDriver.Initialize();
        SequenceDriver.Initialize();
        SequenceStartCmdDriver.Initialize();
        SequenceStopCmdDriver.Initialize();
        SequenceResetCmdDriver.Initialize();
        SceneLoadCmdDriver.Initialize();
        ResetDriver.Initialize();
    }
}
