namespace Atomic.Net.MonoGame.Flex.UI;

public class UIPanel(
    NineSlice nineSlice
) : UINode
{
    private readonly NineSlice _nineSlice = nineSlice;

    private Rectangle _topLeft, _top, _topRight,
                      _left, _center, _right,
                      _bottomLeft, _bottom, _bottomRight;

    protected override void DrawInternal(
        GameTime gameTime, SpriteBatch spriteBatch, float trueLeft, float trueTop
    )
    {
        var left = (int)trueLeft;
        var top = (int)trueTop;
        var width = (int)_node.LayoutGetWidth();
        var height = (int)_node.LayoutGetHeight();

        var size = _nineSlice.SpriteSize;
        var newCenter = new Rectangle(left + size, top + size, width - 2 * size, height - 2 * size);

        if (_center != newCenter)
        {
            var right = left + width;
            var bottom = top + height;

            _topLeft = new Rectangle(left, top, size, size);
            _top = new Rectangle(left + size, top, width - 2 * size, size);
            _topRight = new Rectangle(right - size, top, size, size);

            _left = new Rectangle(left, top + size, size, height - 2 * size);
            _center = newCenter;
            _right = new Rectangle(right - size, top + size, size, height - 2 * size);

            _bottomLeft = new Rectangle(left, bottom - size, size, size);
            _bottom = new Rectangle(left + size, bottom - size, width - 2 * size, size);
            _bottomRight = new Rectangle(right - size, bottom - size, size, size);
        }

        spriteBatch.Draw(_nineSlice.Texture, _topLeft, _nineSlice.TopLeft, Color.White);
        spriteBatch.Draw(_nineSlice.Texture, _top, _nineSlice.Top, Color.White);
        spriteBatch.Draw(_nineSlice.Texture, _topRight, _nineSlice.TopRight, Color.White);
        spriteBatch.Draw(_nineSlice.Texture, _left, _nineSlice.Left, Color.White);
        spriteBatch.Draw(_nineSlice.Texture, _center, _nineSlice.Center, Color.White);
        spriteBatch.Draw(_nineSlice.Texture, _right, _nineSlice.Right, Color.White);
        spriteBatch.Draw(_nineSlice.Texture, _bottomLeft, _nineSlice.BottomLeft, Color.White);
        spriteBatch.Draw(_nineSlice.Texture, _bottom, _nineSlice.Bottom, Color.White);
        spriteBatch.Draw(_nineSlice.Texture, _bottomRight, _nineSlice.BottomRight, Color.White);
    }
}

