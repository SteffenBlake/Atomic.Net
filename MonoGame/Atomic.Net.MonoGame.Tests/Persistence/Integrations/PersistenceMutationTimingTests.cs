using System.Collections.Immutable;
using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Persistence;

namespace Atomic.Net.MonoGame.Tests.Persistence.Integrations;

/// <summary>
/// Integration tests for mutation tracking and save timing edge cases.
/// Tests: Rapid mutations, scene load timing, pre-behavior mutations.
/// </summary>
/// <remarks>
/// test-architect: These tests validate that dirty tracking correctly batches mutations.
/// Critical behavior: Only last value per frame is written to disk.
/// Tests MUST run sequentially due to shared LiteDB database.
/// </remarks>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class PersistenceMutationTimingTests : IDisposable
{
    private readonly string _dbPath;

    public PersistenceMutationTimingTests()
    {
        // Arrange: Initialize systems with clean database
        _dbPath = "persistence.db";

        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        // Clean up entities and database between tests
        EventBus<ShutdownEvent>.Push(new());

        DatabaseRegistry.Instance.Shutdown();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public void RapidMutations_SameFrame_OnlyLastValueWrittenToDisk()
    {
        // Arrange: Create persistent entity
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("rapid-mutation-key");
        });
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("score", 1f) };
        });

        // Act: Rapidly mutate property multiple times in same "frame" (before Flush)
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("score", 5f) };
        });
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("score", 7f) };
        });
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("score", 10f) };
        });

        // test-architect: Only single write should occur (batched)
        DatabaseRegistry.Instance.Flush();

        // Assert: Database should have final value (10), not intermediate values
        var newEntity = EntityRegistry.Instance.Activate();
        newEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("rapid-mutation-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.NotNull(loadedProps.Value.Properties);
        Assert.Equal(10f, loadedProps.Value.Properties["score"]);
    }

    [Fact]
    public void MutationsDuringSceneLoad_AreSavedAfterReEnable()
    {
        // Arrange: Disable dirty tracking to simulate scene load
        DatabaseRegistry.Instance.Disable();

        // Act: Create entity and mutate during disabled period
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("scene-load-mutation-key");
        });
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("counter", 500f) };
        });

        // test-architect: These mutations should NOT be tracked (disabled)
        DatabaseRegistry.Instance.Flush(); // Should be no-op

        // Re-enable and make additional mutations
        DatabaseRegistry.Instance.Enable();

        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("counter", 1000f) };
        });

        // test-architect: This mutation SHOULD be tracked (enabled)
        DatabaseRegistry.Instance.Flush();

        // Assert: Database should have post-enable value (1000)
        var newEntity = EntityRegistry.Instance.Activate();
        newEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("scene-load-mutation-key");
        });
        Assert.True(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out var loadedProps));
        Assert.NotNull(loadedProps.Value.Properties);
        Assert.Equal(1000f, loadedProps.Value.Properties["counter"]);
    }

    [Fact]
    public void MutationsBeforePersistToDiskBehavior_AreNotSaved()
    {
        // Arrange: Disable dirty tracking to simulate scene load
        DatabaseRegistry.Instance.Disable();

        // Act: Create entity and mutate BEFORE adding PersistToDiskBehavior
        var entity = EntityRegistry.Instance.Activate();
        entity.SetBehavior<PropertiesBehavior>(static (ref behavior) =>
        {
            behavior = behavior with { Properties = behavior.Properties.With("value", 999f) };
        });

        // test-architect: Now add PersistToDiskBehavior (still disabled - entity NOT marked dirty)
        entity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("pre-behavior-key");
        });

        // Re-enable tracking
        DatabaseRegistry.Instance.Enable();

        // test-architect: Flush should NOT write anything (entity not dirty)
        DatabaseRegistry.Instance.Flush();

        // Assert: Verify entity was NOT written (no data in DB for this key)
        var newEntity = EntityRegistry.Instance.Activate();
        newEntity.SetBehavior<PersistToDiskBehavior>(static (ref behavior) =>
        {
            behavior = new PersistToDiskBehavior("pre-behavior-key");
        });
        // test-architect: Since no data in DB, entity should NOT have PropertiesBehavior loaded
        Assert.False(BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(newEntity, out _));

        // test-architect: FINDING: This test validates that mutations during disabled period
        // don't get tracked, and entities added during disabled are NOT marked dirty.
    }
}
