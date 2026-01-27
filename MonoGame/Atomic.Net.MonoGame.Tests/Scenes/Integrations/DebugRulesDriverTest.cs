using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Scenes.Integrations;

[Collection("NonParallel")]
public class DebugRulesDriverTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<ErrorEvent> _errors = new();

    public DebugRulesDriverTest(ITestOutputHelper output)
    {
        _output = output;
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        EventBus<ErrorEvent>.Register(new ErrorHandler(_errors, _output));
    }

    public void Dispose()
    {
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void Debug_RulesDriver()
    {
        var scenePath = "Scenes/Fixtures/RulesDriver/simple-mutation.json";
        _output.WriteLine("Loading scene...");
        SceneLoader.Instance.LoadGameScene(scenePath);

        var entities = EntityRegistry.Instance.GetActiveEntities().ToList();
        _output.WriteLine($"Loaded {entities.Count} entities");

        var rules = RuleRegistry.Instance.Rules;
        var ruleCount = 0;
        for (ushort i = 0; i < 1024; i++)
        {
            if (rules.HasValue(i))
            {
                ruleCount++;
                _output.WriteLine($"Rule {i} exists");
            }
        }
        _output.WriteLine($"Total rules: {ruleCount}");

        _output.WriteLine("Running frame...");
        RulesDriver.Instance.RunFrame(0.016f);

        if (_errors.Any())
        {
            foreach (var err in _errors)
            {
                _output.WriteLine($"ERROR: {err.Message}");
            }
        }

        _output.WriteLine($"Properties after RunFrame:");
        foreach (var entity in entities)
        {
            if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var props))
            {
                var p = props.Value.Properties;
                if (p != null)
                {
                    foreach (var kvp in p)
                    {
                        _output.WriteLine($"  {kvp.Key} = {kvp.Value}");
                    }
                }
            }
        }
    }

    private class ErrorHandler : IEventHandler<ErrorEvent>
    {
        private readonly List<ErrorEvent> _errors;
        private readonly ITestOutputHelper _output;

        public ErrorHandler(List<ErrorEvent> errors, ITestOutputHelper output)
        {
            _errors = errors;
            _output = output;
        }

        public void OnEvent(ErrorEvent e)
        {
            _errors.Add(e);
            _output.WriteLine($"ERROR EVENT: {e.Message}");
        }
    }
}
