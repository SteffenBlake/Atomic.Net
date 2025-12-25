namespace Atomic.Net.MonoGame.Flex.UI;

public readonly record struct ThreeSlice
{
    public readonly int SpriteSize;
    public readonly Texture2D Texture;
    public readonly Rectangle Start;
    public readonly Rectangle Middle;
    public readonly Rectangle End;

    public ThreeSlice(
        GraphicsDevice device,
        Texture2D textureSrc,
        int originalSpriteSize,
        float scale,
        Rectangle startSrc,
        Rectangle middleSrc,
        Rectangle endSrc
    )
    {
        SpriteSize = (int)MathF.Round(originalSpriteSize * scale);

        var rt = new RenderTarget2D(device, SpriteSize * 3, SpriteSize);
        using var sb = new SpriteBatch(device);

        device.SetRenderTarget(rt);
        device.Clear(Color.Transparent);
        sb.Begin(samplerState: SamplerState.PointClamp);

        Start = new Rectangle(0, 0, SpriteSize, SpriteSize);
        Middle = new Rectangle(SpriteSize, 0, SpriteSize, SpriteSize);
        End = new Rectangle(SpriteSize * 2, 0, SpriteSize, SpriteSize);

        sb.Draw(textureSrc, Start, startSrc, Color.White);
        sb.Draw(textureSrc, Middle, middleSrc, Color.White);
        sb.Draw(textureSrc, End, endSrc, Color.White);

        sb.End();
        device.SetRenderTarget(null);

        Texture = rt;
    }
}

