using Atomic.Net.MonoGame.Core.Extensions;

namespace Atomic.Net.MonoGame.Flex.UI;

public class UINode
{
    protected readonly Node _node = FlexLayoutSharp.Flex.CreateDefaultNode();

    public IReadOnlyList<UINode> Children => _children;
    protected List<UINode> _children = [];

    public void SetDisplay(Display display) => _node.StyleSetDisplay(display);

    public void SetDirection(FlexDirection direction) => _node.StyleSetFlexDirection(direction);

    public void SetFlexWrap(Wrap flexWrap) => _node.StyleSetFlexWrap(flexWrap);
    
    public void SetJustifyContent(Justify justifyContent) => _node.StyleSetJustifyContent(justifyContent);

    public void SetAlignItems(Align alignItems) => _node.StyleSetAlignItems(alignItems);

    public void SetAlignContent(Align alignContent) => _node.StyleSetAlignContent(alignContent);
    
    public void SetFlexGrow(float flexGrow) => _node.StyleSetFlexGrow(flexGrow);

    public void SetFlexShrink(float flexShrink) => _node.StyleSetFlexShrink(flexShrink);
    
    public void SetAlignSelf(Align alignSelf) => _node.StyleSetAlignSelf(alignSelf);

    public void SetPosition(Edge edge, float position) => _node.StyleSetPosition(edge, position);
    public void SetPositionPercent(Edge edge, float position) => _node.StyleSetPositionPercent(edge, position);

    public void SetPositionType(PositionType positionType) => _node.StyleSetPositionType(positionType);

    public void SetMargin(Edge edge, float margin) => _node.StyleSetMargin(edge, margin);
    public void SetMarginPercent(Edge edge, float margin) => _node.StyleSetMarginPercent(edge, margin);
    
    public void SetBorder(Edge edge, float border) => _node.StyleSetBorder(edge, border);

    public void SetPadding(Edge edge, float padding) => _node.StyleSetPadding(edge, padding);
    public void SetPaddingPercent(Edge edge, float padding) => _node.StyleSetPaddingPercent(edge, padding);

    public void SetWidth(float width) => _node.StyleSetWidth(width);
    public void SetWidthPercent(float width) => _node.StyleSetWidthPercent(width);

    public void SetHeight(float height) => _node.StyleSetHeight(height);
    public void SetHeightPercent(float height) => _node.StyleSetHeightPercent(height);

    public void RecalculateLayout(
        float parentWidth, float parentHeight, Direction direction = Direction.LTR
    ) => _node.CalculateLayout(parentWidth, parentHeight, direction);

    public void AddChild(UINode child)
    {
        _children.Add(child);
        _node.AddChild(child._node);
    }

    public void InsertChild(int index, UINode child)
    {
        _children.Insert(index, child);
        _node.InsertChild(child._node, index);
    }

    public void RemoveChild(UINode child)
    {
        _children.Remove(child);
        _node.RemoveChild(child._node);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, float parentLeft = 0, float parentTop = 0)
    {
        // Left doesnt include Parent's offset, but is inside the margin
        // Width includes margin
        var trueLeft = parentLeft + _node.LayoutGetLeft();
        var trueTop = parentTop + _node.LayoutGetTop();
        var trueWidth = _node.LayoutGetWidth();
        var trueHeight = _node.LayoutGetHeight();

        var marginLeft = trueLeft - _node.LayoutGetMargin(Edge.Left);
        var paddedLeft = trueLeft + _node.LayoutGetPadding(Edge.Left);

        var marginWidth = trueWidth + _node.LayoutGetMargin(Edge.Left) + _node.LayoutGetMargin(Edge.Right);
        var paddedWidth = trueWidth - _node.LayoutGetPadding(Edge.Left) - _node.LayoutGetPadding(Edge.Right);

        var marginTop = trueTop - _node.LayoutGetMargin(Edge.Top);
        var paddedTop = trueTop + _node.LayoutGetPadding(Edge.Top);

        var marginHeight = trueHeight + _node.LayoutGetMargin(Edge.Top) + _node.LayoutGetMargin(Edge.Bottom);
        var paddedHeight = trueHeight - _node.LayoutGetPadding(Edge.Top) - _node.LayoutGetPadding(Edge.Bottom);

        var marginBox = new Rectangle(
            (int)marginLeft, (int)marginTop, (int)marginWidth, (int)marginHeight
        );
        var trueBox = new Rectangle(
            (int)trueLeft , (int)trueTop, (int)trueWidth, (int)trueHeight
        );
        var paddedBox = new Rectangle(
            (int)paddedLeft, (int)paddedTop, (int)paddedWidth, (int)paddedHeight
        );

        DrawInternal(gameTime, spriteBatch, trueLeft, trueTop);

        // spriteBatch.DrawRectangle(marginBox, Color.Red, 1);
        // spriteBatch.DrawRectangle(trueBox, Color.Green, 1);
        // spriteBatch.DrawRectangle(paddedBox, Color.Blue, 1);

        foreach(var child in _children)
        {
            child.Draw(gameTime, spriteBatch, trueLeft, trueTop);
        }
    }

    protected virtual void DrawInternal(
        GameTime gameTime, SpriteBatch spriteBatch, float trueLeft, float trueTop
    )
    {}
}


