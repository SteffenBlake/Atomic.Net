using System.Drawing;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Stores local flex rectangles of an entity, relative to the entity's local coordinate space.
/// Positions are relative to the entity's position, NOT world-space or parent-relative.
/// NOTE: Do not set this behavior manually, it's updated automatically via the <see cref="FlexRegistry"/>
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

