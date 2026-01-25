using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Sprites;

public readonly struct NineSlicer(int size)
{
    public readonly int Size = size;
    public readonly Rectangle TopLeft = new(0, 0, size, size);
    public readonly Rectangle Top = new(size, 0, size, size);
    public readonly Rectangle TopRight = new(size*2, 0, size, size);
    public readonly Rectangle Left = new(0, size, size, size);
    public readonly Rectangle Center = new(size, size, size, size);
    public readonly Rectangle Right = new(size*2, size, size, size);
    public readonly Rectangle BottomLeft = new(0, size*2, size, size);
    public readonly Rectangle Bottom =  new(size, size*2, size, size);
    public readonly Rectangle BottomRight = new(size*2, size*2, size, size);
}

