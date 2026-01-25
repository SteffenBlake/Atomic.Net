using Atomic.Net.MonoGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Atomic.Net.MonoGame.Sprites;

public abstract class NineSliceRegistry(
    string texturePath,
    int sourceSpriteSize,
    float scale = 1.0f,
    byte capacity = byte.MaxValue
) :
    IEventHandler<LoadContentEvent>
{
    private readonly string _texturePath = texturePath;

    private Texture2D? _source;

    private readonly Texture2D[] _textures = new Texture2D[capacity];

    protected SpriteSlicer SourceSlicer { get; } = new SpriteSlicer(sourceSpriteSize);

    private byte _nextIndex = 0;

    public NineSlicer Slicer { get; } = new(
        (int)MathF.Round(sourceSpriteSize * scale)
    );

    public virtual void OnEvent(LoadContentEvent _)
    {
        _source = AtomicGame.Instance.Content.Load<Texture2D>(_texturePath);
    }

    protected byte BuildSlice(
        Rectangle topLeft,
        Rectangle top,
        Rectangle topRight,
        Rectangle left,
        Rectangle center,
        Rectangle right,
        Rectangle bottomLeft,
        Rectangle bottom,
        Rectangle bottomRight
    )
    {
        if (_source is null)
        {
            throw new InvalidOperationException("ThreeSliceRegistry used before LoadContent.");
        }

        if (_nextIndex == _textures.Length)
        {
            throw new InvalidOperationException("ThreeSliceRegistry capacity exceeded.");
        }

        var device = AtomicGame.Instance.GraphicsDevice;

        var size = Slicer.Size * 3;
        var renderTarget = new RenderTarget2D(device, size, size);

        using var spriteBatch = new SpriteBatch(device);

        device.SetRenderTarget(renderTarget);
        device.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

        spriteBatch.Draw(_source, Slicer.TopLeft, topLeft, Color.White);
        spriteBatch.Draw(_source, Slicer.Top, top, Color.White);
        spriteBatch.Draw(_source, Slicer.TopRight, topRight, Color.White);

        spriteBatch.Draw(_source, Slicer.Left, left, Color.White);
        spriteBatch.Draw(_source, Slicer.Center, center, Color.White);
        spriteBatch.Draw(_source, Slicer.Right, right, Color.White);

        spriteBatch.Draw(_source, Slicer.BottomLeft, bottomLeft, Color.White);
        spriteBatch.Draw(_source, Slicer.Bottom, bottom, Color.White);
        spriteBatch.Draw(_source, Slicer.BottomRight, bottomRight, Color.White);

        spriteBatch.End();
        device.SetRenderTarget(null);

        var sliceIndex = _nextIndex;
        _textures[sliceIndex] = renderTarget;

        _nextIndex++;

        return sliceIndex;
    }

    public Texture2D this[byte index] => _textures[index];
}

