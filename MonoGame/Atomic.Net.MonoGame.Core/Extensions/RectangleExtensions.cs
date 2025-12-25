namespace Atomic.Net.Monogame.Core.Extensions;

public static class RectangleExtensions
{
    public static Rectangle[,] SliceTileMatrix(this Rectangle dest, int spriteSize)
    {
        int cols = (dest.Width  + spriteSize - 1) / spriteSize;
        int rows = (dest.Height + spriteSize - 1) / spriteSize;
        var tiles = new Rectangle[rows, cols];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int px = dest.Left + x * spriteSize;
                int py = dest.Top + y * spriteSize;
                int w = Math.Min(spriteSize, dest.Right - px);
                int h = Math.Min(spriteSize, dest.Bottom - py);
                tiles[y, x] = new Rectangle(px, py, w, h);
            }
        }

        return tiles;
    }
}
