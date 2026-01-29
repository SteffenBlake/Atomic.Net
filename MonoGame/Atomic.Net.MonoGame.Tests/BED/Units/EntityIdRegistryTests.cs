using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.BED.Units;

[Collection("NonParallel")]
public sealed class EntityIdRegistryUnitTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;
    public EntityIdRegistryUnitTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        _errorLogger.Dispose();

        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void EntityIdRegistry_WhenIdBehaviorChanges_UpdatesRegistryCorrectly()
    {
        var entity = EntityRegistry.Instance.Activate();
        
        // Set initial ID
        entity.SetBehavior<IdBehavior>(static (ref b) => b = new IdBehavior("initial-id")
        );
        
        Assert.True(EntityIdRegistry.Instance.TryResolve("initial-id", out var resolved1));
        Assert.Equal(entity.Index, resolved1.Value.Index);
        
        // Change ID by removing and re-adding the behavior
        BehaviorRegistry<IdBehavior>.Instance.Remove(entity);
        entity.SetBehavior<IdBehavior>(static (ref b) => b = new IdBehavior("changed-id")
        );
        
        // Old ID should be gone
        Assert.False(EntityIdRegistry.Instance.TryResolve("initial-id", out _));
        
        // New ID should resolve
        Assert.True(EntityIdRegistry.Instance.TryResolve("changed-id", out var resolved2));
        Assert.Equal(entity.Index, resolved2.Value.Index);
    }
}
