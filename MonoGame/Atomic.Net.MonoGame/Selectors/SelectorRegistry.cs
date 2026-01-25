using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Selectors;

public class SelectorRegistry : 
    ISingleton<SelectorRegistry>,
    IEventHandler<InitializeEvent>
{
    public static SelectorRegistry Instance { get; private set; } = null!;
    
    public static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    private readonly Dictionary<string, IdEntitySelector> _idSelectorRegistry = new(Constants.MaxEntities / 64); 

    public void OnEvent(InitializeEvent e)
    {
    }
}

