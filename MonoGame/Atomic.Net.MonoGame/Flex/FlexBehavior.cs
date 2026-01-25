using System.Drawing;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Computed final positions of the Flex node, based on its various behaviors
/// NOTE: Do not set this behavior manually, it's updating is handled automatically via the <see cref="FlexRegistry"/>
/// </summary>
public readonly record struct FlexBehavior(
    RectangleF MarginRect,
    RectangleF PaddingRect, 
    RectangleF ContentRect, 
    float BorderLeft,
    float BorderTop,
    float BorderRight,
    float BorderBottom,
    int ZIndex
);

