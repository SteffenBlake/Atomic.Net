using Atomic.Net.MonoGame.Flex.UI.Text;

namespace Atomic.Net.MonoGame.Flex.UI;

public static class UIExtensions
{
    public static T WithHide<T>(this T self)
        where T : UINode
    {
        self.SetDisplay(Display.None);
        return self;
    }

    public static T WithShow<T>(this T self)
        where T : UINode
    {
        self.SetDisplay(Display.Flex);
        return self;
    }


    public static T WithRow<T>(this T self)
        where T : UINode
    {
        self.SetDirection(FlexDirection.Row);
        return self;
    }

    public static T WithRowReverse<T>(this T self)
        where T : UINode
    {
        self.SetDirection(FlexDirection.RowReverse);
        return self;
    }

    public static T WithColumn<T>(this T self)
        where T : UINode
    {
        self.SetDirection(FlexDirection.Column);
        return self;
    }

    public static T WithColumnReverse<T>(this T self)
        where T : UINode
    {
        self.SetDirection(FlexDirection.ColumnReverse);
        return self;
    }


    public static T WithWrap<T>(this T self)
        where T : UINode
    {
        self.SetFlexWrap(Wrap.Wrap);
        return self;
    }

    public static T WithNoWrap<T>(this T self)
        where T : UINode
    {
        self.SetFlexWrap(Wrap.NoWrap);
        return self;
    }

    public static T WithWrapReverse<T>(this T self)
        where T : UINode
    {
        self.SetFlexWrap(Wrap.WrapReverse);
        return self;
    }


    public static T WithJustifyContentStart<T>(this T self)
        where T : UINode
    {
        self.SetJustifyContent(Justify.FlexStart);
        return self;
    }

    public static T WithJustifyContentCenter<T>(this T self)
        where T : UINode
    {
        self.SetJustifyContent(Justify.Center);
        return self;
    }

    public static T WithJustifyContentEnd<T>(this T self)
        where T : UINode
    {
        self.SetJustifyContent(Justify.FlexEnd);
        return self;
    }

    public static T WithJustifyContentSpaceAround<T>(this T self)
        where T : UINode
    {
        self.SetJustifyContent(Justify.SpaceAround);
        return self;
    }

    public static T WithJustifyContentSpaceBetween<T>(this T self)
        where T : UINode
    {
        self.SetJustifyContent(Justify.SpaceBetween);
        return self;
    }


    public static T WithAlignItemsStart<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.FlexStart);
        return self;
    }

    public static T WithAlignItemsCenter<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.Center);
        return self;
    }

    public static T WithAlignItemsEnd<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.FlexEnd);
        return self;
    }

    public static T WithAlignItemsStretch<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.Stretch);
        return self;
    }

    public static T WithAlignItemsBaseline<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.Baseline);
        return self;
    }


    public static T WithAlignContentStart<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.FlexStart);
        return self;
    }

    public static T WithAlignContentCenter<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.Center);
        return self;
    }

    public static T WithAlignContentEnd<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.FlexEnd);
        return self;
    }

    public static T WithAlignContentStretch<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.Stretch);
        return self;
    }

    public static T WithAlignContentSpaceBetween<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.SpaceBetween);
        return self;
    }

    public static T WithAlignContentSpaceAround<T>(this T self)
        where T : UINode
    {
        self.SetAlignItems(Align.SpaceAround);
        return self;
    }


    public static T WithGrow<T>(this T self, float flexGrow)
        where T : UINode
    {
        self.SetFlexGrow(flexGrow);
        return self;
    }

    public static T WithShrink<T>(this T self, float flexShrink)
        where T : UINode
    {
        self.SetFlexShrink(flexShrink);
        return self;
    }


    public static T WithAlignSelfAuto<T>(this T self)
        where T : UINode
    {
        self.SetAlignSelf(Align.Auto);
        return self;
    }

    public static T WithAlignSelfStart<T>(this T self)
        where T : UINode
    {
        self.SetAlignSelf(Align.FlexStart);
        return self;
    }

    public static T WithAlignSelfCenter<T>(this T self)
        where T : UINode
    {
        self.SetAlignSelf(Align.Center);
        return self;
    }

    public static T WithAlignSelfEnd<T>(this T self)
        where T : UINode
    {
        self.SetAlignSelf(Align.FlexEnd);
        return self;
    }

    public static T WithAlignSelfBaseline<T>(this T self)
        where T : UINode
    {
        self.SetAlignSelf(Align.Baseline);
        return self;
    }

    public static T WithAlignSelfStretch<T>(this T self)
        where T : UINode
    {
        self.SetAlignSelf(Align.Stretch);
        return self;
    }


    public static T WithTop<T>(this T self, float top)
        where T : UINode
    {
        self.SetPosition(Edge.Top, top);
        return self;
    }
    
    public static T WithBottom<T>(this T self, float bottom)
        where T : UINode
    {
        self.SetPosition(Edge.Bottom, bottom);
        return self;
    }
    
    public static T WithLeft<T>(this T self, float left)
        where T : UINode
    {
        self.SetPosition(Edge.Left, left);
        return self;
    }
    
    public static T WithRight<T>(this T self, float right)
        where T : UINode
    {
        self.SetPosition(Edge.Right, right);
        return self;
    }


    public static T WithTopPercent<T>(this T self, float top)
        where T : UINode
    {
        self.SetPositionPercent(Edge.Top, top);
        return self;
    }
    
    public static T WithBottomPercent<T>(this T self, float bottom)
        where T : UINode
    {
        self.SetPositionPercent(Edge.Bottom, bottom);
        return self;
    }
    
    public static T WithLeftPercent<T>(this T self, float left)
        where T : UINode
    {
        self.SetPositionPercent(Edge.Left, left);
        return self;
    }
    
    public static T WithRightPercent<T>(this T self, float right)
        where T : UINode
    {
        self.SetPositionPercent(Edge.Right, right);
        return self;
    }


    public static T WithPositionAbsolute<T>(this T self)
        where T : UINode
    {
        self.SetPositionType(PositionType.Absolute);
        return self;
    }

    public static T WithPositionRelative<T>(this T self)
        where T : UINode
    {
        self.SetPositionType(PositionType.Relative);
        return self;
    }


    public static T WithMarginTop<T>(this T self, float marginTop)
        where T : UINode
    {
        self.SetMargin(Edge.Top, marginTop);
        return self;
    }
    
    public static T WithMarginBottom<T>(this T self, float marginBottom)
        where T : UINode
    {
        self.SetMargin(Edge.Bottom, marginBottom);
        return self;
    }
    
    public static T WithMarginLeft<T>(this T self, float marginLeft)
        where T : UINode
    {
        self.SetMargin(Edge.Left, marginLeft);
        return self;
    }
    
    public static T WithMarginRight<T>(this T self, float marginRight)
        where T : UINode
    {
        self.SetMargin(Edge.Right, marginRight);
        return self;
    }

    public static T WithMarginAll<T>(this T self, float marginAll)
        where T : UINode
    {
        self.SetMargin(Edge.All, marginAll);
        return self;
    }


    public static T WithMarginPercentTop<T>(this T self, float marginTop)
        where T : UINode
    {
        self.SetMarginPercent(Edge.Top, marginTop);
        return self;
    }
    
    public static T WithMarginPercentBottom<T>(this T self, float marginBottom)
        where T : UINode
    {
        self.SetMarginPercent(Edge.Bottom, marginBottom);
        return self;
    }
    
    public static T WithMarginPercentLeft<T>(this T self, float marginLeft)
        where T : UINode
    {
        self.SetMarginPercent(Edge.Left, marginLeft);
        return self;
    }
    
    public static T WithMarginPercentRight<T>(this T self, float marginRight)
        where T : UINode
    {
        self.SetMarginPercent(Edge.Right, marginRight);
        return self;
    }

    public static T WithMarginPercentAll<T>(this T self, float marginAll)
        where T : UINode
    {
        self.SetMarginPercent(Edge.All, marginAll);
        return self;
    }


    public static T WithBorderTop<T>(this T self, float borderTop)
        where T : UINode
    {
        self.SetBorder(Edge.Top, borderTop);
        return self;
    }
    
    public static T WithBorderBottom<T>(this T self, float borderBottom)
        where T : UINode
    {
        self.SetBorder(Edge.Bottom, borderBottom);
        return self;
    }
    
    public static T WithBorderLeft<T>(this T self, float borderLeft)
        where T : UINode
    {
        self.SetBorder(Edge.Left, borderLeft);
        return self;
    }
    
    public static T WithBorderRight<T>(this T self, float borderRight)
        where T : UINode
    {
        self.SetBorder(Edge.Right, borderRight);
        return self;
    }

    public static T WithBorderAll<T>(this T self, float borderAll)
        where T : UINode
    {
        self.SetBorder(Edge.All, borderAll);
        return self;
    }


    public static T WithPaddingTop<T>(this T self, float paddingTop)
        where T : UINode
    {
        self.SetPadding(Edge.Top, paddingTop);
        return self;
    }
    
    public static T WithPaddingBottom<T>(this T self, float paddingBottom)
        where T : UINode
    {
        self.SetPadding(Edge.Bottom, paddingBottom);
        return self;
    }
    
    public static T WithPaddingLeft<T>(this T self, float paddingLeft)
        where T : UINode
    {
        self.SetPadding(Edge.Left, paddingLeft);
        return self;
    }
    
    public static T WithPaddingRight<T>(this T self, float paddingRight)
        where T : UINode
    {
        self.SetPadding(Edge.Right, paddingRight);
        return self;
    }

    public static T WithPaddingAll<T>(this T self, float paddingAll)
        where T : UINode
    {
        self.SetPadding(Edge.All, paddingAll);
        return self;
    }


    public static T WithPaddingPercentTop<T>(this T self, float paddingTop)
        where T : UINode
    {
        self.SetPaddingPercent(Edge.Top, paddingTop);
        return self;
    }
    
    public static T WithPaddingPercentBottom<T>(this T self, float paddingBottom)
        where T : UINode
    {
        self.SetPaddingPercent(Edge.Bottom, paddingBottom);
        return self;
    }
    
    public static T WithPaddingPercentLeft<T>(this T self, float paddingLeft)
        where T : UINode
    {
        self.SetPaddingPercent(Edge.Left, paddingLeft);
        return self;
    }
    
    public static T WithPaddingPercentRight<T>(this T self, float paddingRight)
        where T : UINode
    {
        self.SetPaddingPercent(Edge.Right, paddingRight);
        return self;
    }

    public static T WithPaddingPercentAll<T>(this T self, float paddingAll)
        where T : UINode
    {
        self.SetPaddingPercent(Edge.All, paddingAll);
        return self;
    }


    public static T WithWidth<T>(this T self, float width)
        where T : UINode
    {
        self.SetWidth(width);
        return self;
    }

    public static T WithWidthPercent<T>(this T self, float width)
        where T : UINode
    {
        self.SetWidthPercent(width);
        return self;
    }

    public static T WithHeight<T>(this T self, float height)
        where T : UINode
    {
        self.SetHeight(height);
        return self;
    }

    public static T WithHeightPercent<T>(this T self, float height)
        where T : UINode
    {
        self.SetHeightPercent(height);
        return self;
    }


    public static T WithChild<T>(this T self, UINode child)
        where T : UINode
    {
        self.AddChild(child);
        return self;
    }

    public static T WithChildAt<T>(this T self, int index, UINode child)
        where T : UINode
    {
        self.InsertChild(index, child);
        return self;
    }

    public static T WithChildren<T>(this T self, params UINode[] children)
        where T : UINode
    {
        foreach(var child in children)
        {
            self.AddChild(child);
        }
        return self;
    }

    public static T WithParent<T>(this T self, UINode parent)
        where T : UINode
    {
        parent.AddChild(self);
        return self;
    }


    public static T WithText<T>(this T self, string text)
        where T : IHasText
    {
        self.SetText(text);
        return self;
    }
}
