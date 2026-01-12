using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

public class AtomicEngine : IEventHandler<InitializeEvent>
{
    private readonly GraphicsDeviceManager _graphics;

    public AtomicEngine(bool isFullScreen, DisplayOrientation supportedOrientations)
    {
        EventBus<InitializeEvent>.Register(this);

        _graphics = new(AtomicGame.Instance)
        {
            IsFullScreen = isFullScreen,
            SupportedOrientations = supportedOrientations,
            SynchronizeWithVerticalRetrace = false
        };
        _graphics.ApplyChanges();
    }

    public void OnEvent(InitializeEvent _)
    {
        _graphics.PreferredBackBufferWidth = AtomicGame.Instance.GraphicsDevice.Viewport.Width;
        _graphics.PreferredBackBufferHeight = AtomicGame.Instance.GraphicsDevice.Viewport.Height;
    }
}
