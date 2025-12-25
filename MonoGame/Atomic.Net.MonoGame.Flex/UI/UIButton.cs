namespace Atomic.Net.MonoGame.Flex.UI;

public class UIButton(ThreeSlice idle, ThreeSlice pressed) : UINode
{
    private readonly ThreeSlice _idle = idle;
    private readonly ThreeSlice _pressed = pressed;
    private Rectangle _start, _middle, _end;

    public bool Pressed { get; set; } = false;

    protected override void DrawInternal(
        GameTime gameTime, SpriteBatch spriteBatch, float trueLeft, float trueTop
    )
    {
        var left = (int)trueLeft;
        var top = (int)trueTop;
        var width = (int)_node.LayoutGetWidth();
        var height = (int)_node.LayoutGetHeight();

        var slice = Pressed ? _pressed : _idle;

        var size = slice.SpriteSize;
        var newMiddle = new Rectangle(left + size, top, width - 2 * size, height);

        if (_middle != newMiddle)
        {
            var right = left + width;

            _start = new Rectangle(left, top, size, height);
            _middle = newMiddle;
            _end = new Rectangle(right - size, top, size, height);
        }

        spriteBatch.Draw(slice.Texture, _start, slice.Start, Color.White);
        spriteBatch.Draw(slice.Texture, _middle, slice.Middle, Color.White);
        spriteBatch.Draw(slice.Texture, _end, slice.End, Color.White);
    }
}

