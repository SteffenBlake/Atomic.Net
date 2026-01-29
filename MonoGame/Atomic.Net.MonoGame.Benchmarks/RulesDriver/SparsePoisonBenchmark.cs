using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Tags;
using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.RulesDriver;

/// <summary>
/// Benchmark measuring RulesDriver performance with sparse entity distribution.
/// Simulates realistic poison DOT mechanics with ~967 mutations per frame (7900 entities, ~25% poisoned, ~50% with stacks).
/// </summary>
[MemoryDiagnoser]
public class SparsePoisonBenchmark
{
    // Specification requested 8000, but max scene entities is 7936 (indices 256-8191)
    // Using 7900 to avoid potential edge cases
    private const int TotalEntities = 7900;
    private const int IterationCount = 100;
    private const float DeltaTime = 0.016667f; // 60 FPS

    [GlobalSetup]
    public void GlobalSetup()
    {
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        EventBus<ResetEvent>.Push(new());
    }

    [IterationSetup]
    public void IterationSetup()
    {
        EventBus<ResetEvent>.Push(new());

        var random = new Random(42); // Fixed seed for reproducibility

        // Generate entities programmatically
        for (int i = 0; i < TotalEntities; i++)
        {
            var entity = EntityRegistry.Instance.Activate();

            // ALL entities get health property (0-6000 range, steps of 100)
            var health = random.Next(0, 61) * 100; // Range: 0, 100, 200, ..., 6000
            entity.SetBehavior<PropertiesBehavior>((ref b) => 
                b = b with { Properties = b.Properties.With("health", (PropertyValue)health) }
            );

            // 25% chance to get #poisoned tag
            bool isPoisoned = random.NextDouble() < 0.25;
            if (isPoisoned)
            {
                // Add #poisoned tag
                entity.SetBehavior<TagsBehavior>((ref b) => 
                    b = b with { Tags = b.Tags.With("poisoned") }
                );

                // 50% of poisoned entities get poisonStacks (90-100 range)
                if (random.NextDouble() < 0.5)
                {
                    var poisonStacks = random.Next(90, 101);
                    entity.SetBehavior<PropertiesBehavior>((ref b) => 
                        b = b with { Properties = b.Properties.With("poisonStacks", (PropertyValue)poisonStacks) }
                    );
                }
            }
        }

        // Load the rule JSON
        SceneLoader.Instance.LoadGameScene("RulesDriver/Fixtures/sparse-poison-rule.json");
    }

    [Benchmark]
    public void RunPoisonSimulation()
    {
        // Execute 100 iterations of poison damage
        // Simulates ~1.67 seconds of gameplay (100 frames at 60 FPS)
        for (int i = 0; i < IterationCount; i++)
        {
            Scenes.RulesDriver.Instance.RunFrame(DeltaTime);
        }
    }
}
