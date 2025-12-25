using Atomic.Net.MonoGame.Core.Extensions;

namespace Atomic.Net.MonoGame.Flex.UI.Text;

public class UIMultiLineText : UINode
{
    public UIMultiLineTextContent Content { get; }

    public UIMultiLineText(SpriteFont font)
    {
        Content = new UIMultiLineTextContent(font).WithParent(this);

        _node.StyleSetFlex(1);
        _node.StyleSetAlignItems(Align.Center);
        _node.StyleSetJustifyContent(Justify.Center);
    }
}

public class UIMultiLineTextContent : UINode, IHasText
{
    public UIMultiLineTextContent(SpriteFont font) : base()
    {
        _font = font;
        _node.StyleSetHeightPercent(100);
        _node.StyleSetWidthPercent(100);
    }

    public string Text { get; private set; } = "";

    public Color Color { get; set; } = Color.Black;

    public void SetText(string text)
    {
        Text = text.Trim();
        // Trigger a re-cache 
        _bounds = default;
    }

    private Rectangle _bounds;
    private Vector2 _charSize;
    private WordBreakResult _wordBreakResult;
    private readonly SpriteFont _font;

    protected override void DrawInternal(
        GameTime gameTime, SpriteBatch spriteBatch, float trueLeft, float trueTop
    )
    {
        var left = trueLeft + _node.LayoutGetPadding(Edge.Left);
        var top = trueTop+ _node.LayoutGetPadding(Edge.Top);
        var width =
            _node.LayoutGetWidth()
            - _node.LayoutGetPadding(Edge.Left)
            - _node.LayoutGetPadding(Edge.Right);

        var height =
            _node.LayoutGetHeight()
            - _node.LayoutGetPadding(Edge.Top)
            - _node.LayoutGetPadding(Edge.Bottom);

        if (width <= 0)
        {
            return;
        }
        if (height <= 0)
        {
            return;
        }
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        var justification = _node.StyleGetJustifyContent();

        // Align.Auto, Align.FlexStart, Align.FlexEnd, Align.Center, Align.Baseline, Align.SpaceAround, Align.SpaceBetween
        var alignment = _node.StyleGetAlignItems();

        var newBounds = new Rectangle((int)left, (int)top, (int)width, (int)height);
        // Check for re-cache
        if (_bounds != newBounds)
        {
            _bounds = newBounds;
            _charSize = _font.MeasureString("A");
            _wordBreakResult = WordBreaker.FitToBoundingBoxV2(Text, _charSize, new(width, height));
        }

        var scale = _wordBreakResult.ScaleFactor;
        var lines = _wordBreakResult.Ranges.Count;
        var totalTextHeight = lines * _charSize.Y * scale;

        var contentOffsetY = alignment switch
        {
            Align.FlexStart or Align.Auto => 0f,
            Align.FlexEnd => height - totalTextHeight,
            Align.Center or Align.SpaceBetween or Align.SpaceAround => (height - totalTextHeight) / 2f,
            _ => 0f
        };

        for (var i = 0; i < lines; i++)
        {
            var lineRange = _wordBreakResult.Ranges[i];
            var lineText = Text[lineRange].ToString();

            var lineWidth = lineText.Length * _charSize.X * scale;

            var lineOffsetX = justification switch
            {
                Justify.FlexStart => 0f,
                Justify.FlexEnd => width - lineWidth,
                _ => (width - lineWidth) / 2f
            };

            var lineOffsetY = contentOffsetY + i * _charSize.Y * scale;

            try
            {
                spriteBatch.DrawString(
                    _font,
                    lineText,
                    new Vector2(left + lineOffsetX, top + lineOffsetY),
                    Color,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
            catch (ArgumentException)
            {
                if (!Text.TrySanitizeText(_font.Characters, out var sanitized))
                {
                    throw;
                }
                Text = sanitized;
            }
        }
    }
}
