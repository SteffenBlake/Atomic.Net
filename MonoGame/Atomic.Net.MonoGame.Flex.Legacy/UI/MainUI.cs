using Atomic.Net.MonoGame.Core.Sprites;
using Atomic.Net.MonoGame.Flex.UI;
using Atomic.Net.MonoGame.Flex.UI.Text;

namespace Atomic.Net.MonoGame.Flex.UI;

public class MainUI 
{
    private GraphicsDevice? _graphicsDevice;
    private Texture2D? _Texture;
    private SpriteFont? _Font;
    private SpriteBatch? _spriteBatch;

    private Rectangle _viewPort = Rectangle.Empty;

    private UINode? _rootNode;

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _Texture = content.Load<Texture2D>("Textures/Spritesheet_UI_Flat");
        _Font = content.Load<SpriteFont>("Fonts/FiraMono");
        _spriteBatch = new(graphicsDevice);

        var slicer = new SpriteSlicer(16);
        var panelSlice = new NineSlice(
            graphicsDevice, _Texture, slicer.Size, scale: 6.0f, 
            slicer[2, 2], slicer[3, 2], slicer[7, 2],
            slicer[2, 3], slicer[3, 3], slicer[7, 3],
            slicer[2, 5], slicer[3, 5], slicer[7, 5]
        );
        var buttonIdleSlice = new ThreeSlice(
            graphicsDevice, _Texture, slicer.Size, scale: 6.0f,
            slicer[35, 14], slicer[36, 14], slicer[37, 14]
        );
        var buttonPressedSlice = new ThreeSlice(
            graphicsDevice, _Texture, slicer.Size, scale: 6.0f,
            slicer[35, 15], slicer[36, 15], slicer[37, 15]
        );

        _rootNode = new UINode()
            .WithLeft(0)
            .WithTop(0)
            .WithRow()
            .WithJustifyContentStart()
            .WithAlignItemsStretch();

        var leftPanel = new UIPanel(panelSlice)
            .WithGrow(2)
            .WithMarginAll(16)
            .WithColumn()
            .WithParent(_rootNode);

        var leftText = new UIMultiLineText(_Font)
            .WithParent(leftPanel)
            .Content
            .WithText("""Alice was beginning to get very tired of sitting by her sister on the bank, and of having nothing to do: once or twice she had peeped into the book her sister was reading, but it had no pictures or conversations in it, "and what is the use of a book," thought Alice "without pictures or conversations?" """)
            .WithPaddingPercentAll(5);

        var middlePanel = new UIPanel(panelSlice)
            .WithGrow(1)
            .WithMarginAll(16)
            .WithColumn()
            .WithParent(_rootNode);

        var middleText = new UIMultiLineText(_Font)
            .WithParent(middlePanel)
            .Content
            .WithText("""So she was considering in her own mind (as well as she could, for the hot day made her feel very sleepy and stupid), whether the pleasure of making a daisy-chain would be worth the trouble of getting up and picking the daisies, when suddenly a White Rabbit with pink eyes ran close by her.""")
            .WithJustifyContentCenter()
            .WithPaddingPercentAll(5);
        
        var rightNode = new UINode()
            .WithGrow(1)
            .WithColumn()
            .WithParent(_rootNode);       
        
        var topRightPanel = new UIPanel(panelSlice)
            .WithGrow(2)
            .WithMarginAll(16)
            .WithParent(rightNode);

        var topRightText = new UIMultiLineText(_Font)
            .WithParent(topRightPanel)
            .Content
            .WithText("""There was nothing so very remarkable in that; nor did Alice think it so very much out of the way to hear the Rabbit say to itself, "Oh dear! Oh dear! I shall be late!" (when she thought it over afterwards, it occurred to her that she ought to have wondered at this, but at the time it all seemed quite natural); but when the Rabbit actually took a watch out of its waistcoat-pocket, and looked at it, and then hurried on, Alice started to her feet, for it flashed across her mind that she had never before seen a rabbit with either a waistcoat-pocket, or a watch to take out of it, and burning with curiosity, she ran across the field after it, and fortunately was just in time to see it pop down a large rabbit-hole under the hedge.""")
            .WithJustifyContentEnd()
            .WithPaddingPercentAll(5);

        var middleRightPanel = new UIPanel(panelSlice)
            .WithGrow(1)
            .WithMarginAll(16)
            .WithParent(rightNode);

        var middleRightText = new UIMultiLineText(_Font)
            .WithParent(middleRightPanel)
            .Content
            .WithText("""In another moment down went Alice after it, never once considering how in the world she was to get out again.""")
            .WithAlignItemsCenter()
            .WithPaddingPercentAll(5);
       
        var buttonWrapper = new UINode()
            .WithRow()
            .WithJustifyContentCenter()
            .WithMarginAll(16)
            .WithParent(rightNode);

        var bottomRightButton = new UIButton(buttonIdleSlice, buttonPressedSlice)
            .WithRow()
            .WithJustifyContentCenter()
            .WithAlignItemsCenter()
            .WithShrink(1)
            .WithParent(buttonWrapper);

        var buttonText = new UIFixedText(_Font)
            .WithParent(bottomRightButton)
            .Content
            .WithMarginAll(15)
            .WithText("Press Me");
    }

    internal void Draw(GameTime gameTime)
    {
        var viewport = _graphicsDevice!.PresentationParameters.Bounds;
        if (viewport != _viewPort)
        {
            _viewPort = viewport;
            _rootNode!
                .WithWidth(_viewPort.Width)
                .WithHeight(_viewPort.Height)
                .RecalculateLayout(_viewPort.Width, _viewPort.Height);
        }

        _spriteBatch!.Begin(samplerState: SamplerState.PointWrap);

        _rootNode!.Draw(gameTime, _spriteBatch);

        _spriteBatch.End();
    }
}
