using System.Drawing;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Stores the final world-space flex rectangles of an entity, calculated from WorldTransform + FlexBehavior.
/// NOTE: Do not set this behavior manually, it's updated automatically via the <see cref="WorldFlexRegistry"/>
/// </summary>
public struct WorldFlexBehavior
{
    public RectangleF MarginRect;
    public RectangleF PaddingRect;
    public RectangleF ContentRect;
    public float BorderLeft;
    public float BorderTop;
    public float BorderRight;
    public float BorderBottom;
    public int ZIndex;

    public WorldFlexBehavior()
    {
        MarginRect = RectangleF.Empty;
        PaddingRect = RectangleF.Empty;
        ContentRect = RectangleF.Empty;
        BorderLeft = 0;
        BorderTop = 0;
        BorderRight = 0;
        BorderBottom = 0;
        ZIndex = 0;
    }
}
