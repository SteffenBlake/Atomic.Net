namespace Atomic.Net.MonoGame.Flex.UI;

public readonly record struct NineSlice
{
    public readonly int SpriteSize;
    
    public readonly Texture2D Texture;

    public readonly Rectangle TopLeft;
    public readonly Rectangle Top;
    public readonly Rectangle TopRight;
    public readonly Rectangle Left;
    public readonly Rectangle Center;
    public readonly Rectangle Right;
    public readonly Rectangle BottomLeft;
    public readonly Rectangle Bottom;
    public readonly Rectangle BottomRight;

    public NineSlice(
        GraphicsDevice device,
        Texture2D textureSrc,
        int originalSpriteSize,
        float scale,
        Rectangle topLeftSrc,
        Rectangle topSrc,
        Rectangle topRightSrc,
        Rectangle leftSrc,
        Rectangle centerSrc,
        Rectangle rightSrc,
        Rectangle bottomLeftSrc,
        Rectangle bottomSrc,
        Rectangle bottomRightSrc
    )
    {
        SpriteSize = (int)MathF.Round(originalSpriteSize * scale);

        var rt = new RenderTarget2D(device, SpriteSize*3, SpriteSize*3);
        using var sb = new SpriteBatch(device);

        device.SetRenderTarget(rt);
        device.Clear(Color.Transparent);

        sb.Begin(samplerState: SamplerState.PointClamp);

        TopLeft = new Rectangle(0, 0, SpriteSize, SpriteSize);
        Top = new Rectangle(SpriteSize, 0, SpriteSize, SpriteSize);
        TopRight = new Rectangle(SpriteSize * 2, 0, SpriteSize, SpriteSize);
        Left = new Rectangle(0, SpriteSize, SpriteSize, SpriteSize);
        Center = new Rectangle(SpriteSize, SpriteSize, SpriteSize, SpriteSize);
        Right = new Rectangle(SpriteSize * 2, SpriteSize, SpriteSize, SpriteSize);
        BottomLeft = new Rectangle(0, SpriteSize * 2, SpriteSize, SpriteSize);
        Bottom = new Rectangle(SpriteSize, SpriteSize * 2, SpriteSize, SpriteSize);
        BottomRight = new Rectangle(SpriteSize * 2, SpriteSize * 2, SpriteSize, SpriteSize);

        sb.Draw(textureSrc, TopLeft, topLeftSrc, Color.White);
        sb.Draw(textureSrc, Top, topSrc, Color.White);
        sb.Draw(textureSrc, TopRight, topRightSrc, Color.White);
        sb.Draw(textureSrc, Left, leftSrc, Color.White);
        sb.Draw(textureSrc, Center, centerSrc, Color.White);
        sb.Draw(textureSrc, Right, rightSrc, Color.White);
        sb.Draw(textureSrc, BottomLeft, bottomLeftSrc, Color.White);
        sb.Draw(textureSrc, Bottom, bottomSrc, Color.White);
        sb.Draw(textureSrc, BottomRight, bottomRightSrc, Color.White);

        sb.End();
        device.SetRenderTarget(null);

        Texture = rt;
    }
}
