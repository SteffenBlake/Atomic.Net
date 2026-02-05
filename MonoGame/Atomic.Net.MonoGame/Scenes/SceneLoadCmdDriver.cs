using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes scene-load commands by triggering async scene loading.
/// </summary>
public sealed class SceneLoadCmdDriver : ISingleton<SceneLoadCmdDriver>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
    }

    public static SceneLoadCmdDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Start async scene loading from specified path.
    /// </summary>
    /// <param name="scenePath">Path to scene JSON file</param>
    public void Execute(string scenePath)
    {
        SceneManager.Instance.LoadScene(scenePath);
    }
}
