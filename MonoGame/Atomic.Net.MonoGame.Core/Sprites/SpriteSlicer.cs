namespace Atomic.Net.MonoGame.Core.Sprites;

public class SpriteSlicer(int size)
{
    public int Size => size;
    public Rectangle this[int x, int y]
        => new(x * size, y * size, size, size);
}
