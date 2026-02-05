namespace Atomic.Net.MonoGame.Core;

public delegate double TransformFunction(double progress);

public class Transforms
{
    public static readonly TransformFunction Linear =
        t => t;
    public static readonly TransformFunction EaseInQuad =
        t => t * t;
    public static readonly TransformFunction EaseOutQuad =
        t => t * (2 - t);
    public static readonly TransformFunction EaseInOutQuad =
        t => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
}
