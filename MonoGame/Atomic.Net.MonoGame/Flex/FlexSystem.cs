using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Flex;

public static class FlexSystem
{
    public static void Initialize()
    {
        BehaviorRegistry<AlignContentBehavior>.Initialize();
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
        FlexRegistry.Initialize();
    }
}
