using Atomic.Net.MonoGame.Core.Sequencing;
using Microsoft.Xna.Framework;
using R3;

namespace Atomic.Net.MonoGame.Engine;

public class SceneManager : IDisposable
{
    private static SceneManager? _instance;
    public static SceneManager Instance => _instance ??= new();

    private readonly Sequence _transition = new();
    private IDisposable? _currentDisposable;
    private Node? _currentScene;
    private Node? _nextScene;

    public SceneManager()
    {
        _transition
            .Where(() => _nextScene != null)
            .ThenTween(Tween.Out(1.0), (v) => FadeAlpha = v)
            .Then(UnloadScene)
            .ThenTween(Tween.In(1.0), v => FadeAlpha = v)
            .Then(RunGC)
            .ThenTween(Tween.Out(1.0), (v) => FadeAlpha = v)
            .Then(LoadScene)
            .ThenTween(Tween.In(1.0), v => FadeAlpha = v)
            .Then(FinishTranstion);
    }

    public Node? CurrentScene => _currentScene ?? AtomicGameConfig.LoadingScene;

    public double FadeAlpha { get; private set; } = 0.0; // Start totally black

    public void Initialize()
    {
        if (AtomicGameConfig.LoadingScene == null)
        {
            throw new NullReferenceException(nameof(AtomicGameConfig.LoadingScene));
        }
        if (AtomicGameConfig.InitialScene == null)
        {
            throw new NullReferenceException(nameof(AtomicGameConfig.InitialScene));
        }

        SetScene(AtomicGameConfig.InitialScene);
    }

    public void SetScene(Node next)
    {
        _nextScene = next;
    }

    public void Update(GameTime gameTime)
    {
        CurrentScene!.Update(gameTime);
        _transition.Update(gameTime.ElapsedGameTime.TotalSeconds);
    }

    public void Draw(GameTime gameTime)
    {
        CurrentScene!.Draw(gameTime);
    }

    private void UnloadScene()
    {
        if (_currentScene != null && _currentDisposable != null)
        {
            _currentDisposable.Dispose();
            _currentScene = null;
        }
    }

    private void FinishTranstion()
    {
        _nextScene = null;
        _transition.Reset();
    }

    private static void RunGC()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void LoadScene()
    {
        _currentScene = _nextScene ?? throw new InvalidOperationException();
        _currentDisposable = _currentScene.Compose();
    }

    public void Dispose() => _currentDisposable?.Dispose();
}

