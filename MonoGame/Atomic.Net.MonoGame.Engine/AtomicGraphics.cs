using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Engine;

public class AtomicGraphics(Game game) : GraphicsDeviceManager(game)
{
    private static AtomicGraphics? _instance;
    public static AtomicGraphics Instance => _instance ??= new(AtomicGame.Instance);
}
