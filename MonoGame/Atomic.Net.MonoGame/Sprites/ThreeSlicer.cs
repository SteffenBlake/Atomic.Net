using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Sprites;

public readonly struct ThreeSlicer(int size)
{
    public readonly int Size = size;
    public readonly Rectangle Start = new(0, 0, size, size);
    public readonly Rectangle Middle = new(size, 0, size, size);
    public readonly Rectangle End = new(size*2, 0, size, size);
}
