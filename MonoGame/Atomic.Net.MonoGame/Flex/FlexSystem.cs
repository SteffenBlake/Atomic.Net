using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Flex;

public static class FlexSystem
{
    public static void Initialize()
    {
        BehaviorRegistry<AlignContentBehavior>.Initialize();
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
        BehaviorRegistry<WorldFlexBehavior>.Initialize();
        FlexRegistry.Initialize();
        WorldFlexRegistry.Initialize();
    }
}
