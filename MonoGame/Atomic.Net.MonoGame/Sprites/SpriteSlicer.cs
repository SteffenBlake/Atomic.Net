using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Sprites;

public readonly record struct SpriteSlicer(int Size)
{
    public Rectangle this[int x, int y]
        => new(x * Size, y * Size, Size, Size);
}
