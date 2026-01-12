namespace Atomic.Net.MonoGame.Flex.UI.Text;

public readonly struct WordBreakResult(IReadOnlyList<Range> ranges, float scaleFactor)
{
    public readonly IReadOnlyList<Range> Ranges = ranges;
    public readonly float ScaleFactor = scaleFactor;
}

