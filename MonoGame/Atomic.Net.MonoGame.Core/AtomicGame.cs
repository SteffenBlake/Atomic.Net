using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

public class AtomicGame : Game
{
    public static AtomicGame Instance { get; } = new()
    {
        IsFixedTimeStep = false
    };

    protected override void Initialize()
    {
        EventBus<InitializeEvent>.Push(new());
    }

    protected override void LoadContent()
    {
        EventBus<LoadContentEvent>.Push(new());
    }

    protected override void Draw(GameTime gameTime)
    {
        EventBus<DrawFrameEvent>.Push(new(gameTime.ElapsedGameTime));
    }

    protected override void Update(GameTime gameTime)
    {
        EventBus<UpdateFrameEvent>.Push(new(gameTime.ElapsedGameTime));
    }
}

