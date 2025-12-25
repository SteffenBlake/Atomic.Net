using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Engine;

public class AtomicGame : Game, IDisposable
{
    private static AtomicGame? _instance;
    public static AtomicGame Instance => _instance ??= new();

    protected override void Initialize()
    {
        SceneManager.Instance.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SceneManager.Instance.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        SceneManager.Instance.Draw(gameTime);
    }
}

