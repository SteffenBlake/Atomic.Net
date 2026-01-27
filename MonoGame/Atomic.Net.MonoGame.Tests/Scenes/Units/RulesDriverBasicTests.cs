using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.Scenes.Units;

/// <summary>
/// Unit tests for RulesDriver basic functionality.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class RulesDriverBasicTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;

    public RulesDriverBasicTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        EventBus<ShutdownEvent>.Push(new());
        _errorLogger.Dispose();
    }

    [Fact]
    public void SceneLoader_LoadsRulesDriverTestScene_Successfully()
    {
        // Arrange
        var scenePath = "Scenes/Tests/RulesDriver/Aggregates/sum-total-enemy-health.json";
        
        // Act
        SceneLoader.Instance.LoadGameScene(scenePath);
        
        // Assert
        var activeEntities = EntityRegistry.Instance.GetActiveEntities().ToList();
        
        // senior-dev: Should have loaded 3 entities (enemy1, enemy2, enemy3)
        Assert.True(activeEntities.Count >= 3, $"Expected at least 3 entities, got {activeEntities.Count}");
        
        // senior-dev: Should have loaded at least 1 rule
        var ruleCount = 0;
        for (ushort i = 0; i < 1024; i++) // Constants.MaxRules
        {
            if (RuleRegistry.Instance.Rules.HasValue(i))
            {
                ruleCount++;
            }
        }
        Assert.True(ruleCount >= 1, $"Expected at least 1 rule, got {ruleCount}");
    }
}
