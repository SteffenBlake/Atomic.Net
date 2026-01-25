using Atomic.Net.MonoGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Atomic.Net.MonoGame.Sprites;

public abstract class ThreeSliceRegistry(
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

    protected SpriteSlicer SourceSlicer { get; } = new(sourceSpriteSize);

    private byte _nextIndex = 0;

    public ThreeSlicer Slicer { get; } = new(
        (int)MathF.Round(sourceSpriteSize * scale)
    );

    public virtual void OnEvent(LoadContentEvent _)
    {
        _source = AtomicGame.Instance.Content.Load<Texture2D>(_texturePath);
    }

    protected byte BuildSlice(
        Rectangle start,
        Rectangle middle,
        Rectangle end
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

        var width = Slicer.Size * 3;
        var height = Slicer.Size;

        var renderTarget = new RenderTarget2D(device, width, height);

        using var spriteBatch = new SpriteBatch(device);

        device.SetRenderTarget(renderTarget);
        device.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

        spriteBatch.Draw(_source, Slicer.Start, start, Color.White);
        spriteBatch.Draw(_source, Slicer.Middle, middle, Color.White);
        spriteBatch.Draw(_source, Slicer.End, end, Color.White);

        spriteBatch.End();
        device.SetRenderTarget(null);

        var index = _nextIndex;
        _textures[index] = renderTarget;
        _nextIndex++;

        return index;
    }

    public Texture2D this[byte index] => _textures[index];
}


