using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;

namespace Atomic.Net.MonoGame.Sprites;

/// <summary>
/// Global registry for named <see cref="SpriteBatch"/> instances.
/// Intended for pre-allocated sprite batches that are reused during rendering.
/// </summary>
public static class SpriteBatchRegistry
{
    private static readonly Dictionary<string, SpriteBatch> _spriteBatches = [];

    /// <summary>
    /// Attempts to retrieve a sprite batch by key.
    /// </summary>
    /// <param name="key">The unique identifier of the sprite batch.</param>
    /// <param name="spriteBatch">
    /// When this method returns <c>true</c>, contains the associated <see cref="SpriteBatch"/>.
    /// Otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if a sprite batch with the given key exists; otherwise, <c>false</c>.</returns>
    public static bool TryGet(
        string key, 
        [NotNullWhen(true)]
        out SpriteBatch? spriteBatch
    )
    {
        return _spriteBatches.TryGetValue(key, out spriteBatch);
    }

    /// <summary>
    /// Registers a sprite batch with the given key.
    /// </summary>
    /// <param name="key">The unique identifier for the sprite batch.</param>
    /// <param name="graphicsDevice">The Graphics Device for the instance</param>
    public static void Add(string key, GraphicsDevice graphicsDevice)
    {
        _spriteBatches.Add(key, new SpriteBatch(graphicsDevice));
    }

    /// <summary>
    /// Removes a sprite batch from the registry.
    /// </summary>
    /// <param name="key">The key of the sprite batch to remove.</param>
    /// <returns><c>true</c> if the sprite batch was removed; otherwise, <c>false</c>.</returns>
    public static bool Remove(string key)
    {
        return _spriteBatches.Remove(key);
    }
}
