using System.Drawing;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Stores the final world-space flex rectangles of an entity, calculated from WorldTransform + FlexBehavior.
/// NOTE: Do not set this behavior manually, it's updated automatically via the <see cref="WorldFlexRegistry"/>
/// </summary>
public readonly record struct WorldFlexBehavior(
    RectangleF MarginRect,
    RectangleF PaddingRect,
    RectangleF ContentRect,
    float BorderLeft,
    float BorderTop,
    float BorderRight,
    float BorderBottom,
    int ZIndex
);
