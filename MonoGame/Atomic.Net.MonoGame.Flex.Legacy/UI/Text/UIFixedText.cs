using Atomic.Net.MonoGame.Core.Extensions;

namespace Atomic.Net.MonoGame.Flex.UI.Text;

public class UIFixedText : UINode
{
    public UIFixedTextContent Content { get; }

    public UIFixedText(SpriteFont font)
    {
        Content = new UIFixedTextContent(font)
            .WithParent(this);

        _node.StyleSetFlexShrink(1);
        _node.StyleSetAlignItems(Align.Center);
        _node.StyleSetJustifyContent(Justify.Center);
    }
}

public class UIFixedTextContent(SpriteFont font) : UINode, IHasText
{
    private readonly SpriteFont _font = font;

    public string Text { get; private set; } = "";

    public Color Color { get; set; } = Color.Black;

    public void SetText(string text)
    {
        if (Text == text)
        {
            return;
        }
        Text = text;

        var size = _font.MeasureString(text);
        _node.StyleSetWidth(size.X);
        _node.StyleSetHeight(size.Y);
    }

    protected override void DrawInternal(
        GameTime gameTime, SpriteBatch spriteBatch, float trueLeft, float trueTop
    )
    {
        try
        {
            spriteBatch.DrawString(_font, Text, new(trueLeft, trueTop), Color);
        }
        catch (ArgumentException)
        {
            if (!Text.TrySanitizeText(_font.Characters, out var sanitized))
            {
                throw;
            }
            Text = sanitized.Value;
        }
    }
}
