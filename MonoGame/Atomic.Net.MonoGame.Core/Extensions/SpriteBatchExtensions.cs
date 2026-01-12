using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Atomic.Net.MonoGame.Core.Extensions;

public static class SpriteBatchExtensions 
{
    private static Texture2D? pixel;

    private static void CreateThePixel(SpriteBatch spriteBatch)
    {
        pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        pixel.SetData([Color.White]);
    }

    /// <summary>
    /// Draws a rectangle with the thickness provided
    /// </summary>
    /// <param name="spriteBatch">The destination drawing surface</param>
    /// <param name="rect">The rectangle to draw</param>
    /// <param name="color">The color to draw the rectangle in</param>
    /// <param name="thickness">The thickness of the lines</param>
    public static void DrawRectangle(this SpriteBatch spriteBatch, Rectangle rect, Color color, float thickness)
    {
        DrawLine(spriteBatch, new Vector2(rect.X, rect.Y), new Vector2(rect.Right, rect.Y), color, thickness); // top
        DrawLine(spriteBatch, new Vector2(rect.X + 1f, rect.Y), new Vector2(rect.X + 1f, rect.Bottom + thickness), color, thickness); // left
        DrawLine(spriteBatch, new Vector2(rect.X, rect.Bottom), new Vector2(rect.Right, rect.Bottom), color, thickness); // bottom
        DrawLine(spriteBatch, new Vector2(rect.Right + 1f, rect.Y), new Vector2(rect.Right + 1f, rect.Bottom + thickness), color, thickness); // right
    }

    /// <summary>
    /// Draws a line from point1 to point2 with an offset
    /// </summary>
    /// <param name="spriteBatch">The destination drawing surface</param>
    /// <param name="point1">The first point</param>
    /// <param name="point2">The second point</param>
    /// <param name="color">The color to use</param>
    /// <param name="thickness">The thickness of the line</param>
    public static void DrawLine(this SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color, float thickness)
    {
        // calculate the distance between the two vectors
        float distance = Vector2.Distance(point1, point2);

        // calculate the angle between the two vectors
        float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);

        DrawLine(spriteBatch, point1, distance, angle, color, thickness);
    }


    /// <summary>
    /// Draws a line from point1 to point2 with an offset
    /// </summary>
    /// <param name="spriteBatch">The destination drawing surface</param>
    /// <param name="point">The starting point</param>
    /// <param name="length">The length of the line</param>
    /// <param name="angle">The angle of this line from the starting point</param>
    /// <param name="color">The color to use</param>
    /// <param name="thickness">The thickness of the line</param>
    public static void DrawLine(this SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color, float thickness)
    {
        if (pixel == null)
        {
            CreateThePixel(spriteBatch);
        }

        // stretch the pixel between the two vectors
        spriteBatch.Draw(
            pixel,
            point,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(length, thickness),
            SpriteEffects.None,
            0
        );
    }

    public static void DrawTiledMatrix(
        this SpriteBatch spriteBatch, 
        Texture2D texture, 
        Rectangle[,] tiles, 
        Rectangle sourceRectangle, 
        Color color
    )
    {
        for (int y = 0; y < tiles.GetLength(0); y++)
            for (int x = 0; x < tiles.GetLength(1); x++)
            {
                var dest = tiles[y, x];
                spriteBatch.Draw(texture, dest, sourceRectangle, color);
            }
    }
 
}
