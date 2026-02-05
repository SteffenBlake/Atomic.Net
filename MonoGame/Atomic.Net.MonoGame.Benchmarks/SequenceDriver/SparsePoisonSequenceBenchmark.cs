using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Tags;
using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.SequenceDriver;

/// <summary>
/// Benchmark measuring SequenceDriver performance with sparse entity distribution.
/// Identical to SparsePoisonBenchmark but uses repeating sequences instead of rules.
/// Frame 1: RulesDriver starts sequences and sequences remove #poisoned tags.
/// Frames 2-100: RulesDriver is no-op (no #poisoned tags), SequenceDriver handles poison ticks.
/// </summary>
[MemoryDiagnoser]
public class SparsePoisonSequenceBenchmark
{
    // Specification requested 8000, but max scene entities is 7936 (indices 256-8191)
    // Using 7900 to avoid potential edge cases
    private const int TotalEntities = 7900;
    private const int IterationCount = 100;
    private const float DeltaTime = 0.016669f; // 60 FPS

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

        var random = new Random(42); // Fixed seed for reproducibility (same as sparse-poison-rule)

        // Generate entities programmatically
        for (int i = 0; i < TotalEntities; i++)
        {
            var entity = EntityRegistry.Instance.Activate();

            // ALL entities get health property (0-6000 range, steps of 100)
            var health = random.Next(0, 61) * 100; // Range: 0, 100, 200, ..., 6000
            entity.SetBehavior<PropertiesBehavior, int>(
                in health,
                static (ref readonly h, ref b) =>
                    b = b with { Properties = b.Properties.With("health", (PropertyValue)h) }
            );

            // 25% chance to get #poisoned tag
            bool isPoisoned = random.NextDouble() < 0.25;
            if (isPoisoned)
            {
                // Add #poisoned tag
                entity.SetBehavior<TagsBehavior>(static (ref b) =>
                    b = b with { Tags = b.Tags.With("poisoned") }
                );

                // 50% of poisoned entities get poisonStacks (90-100 range)
                if (random.NextDouble() < 0.5)
                {
                    var poisonStacks = random.Next(90, 101);
                    entity.SetBehavior<PropertiesBehavior, int>(
                        in poisonStacks,
                        static (ref readonly ps, ref b) =>
                            b = b with { Properties = b.Properties.With("poisonStacks", (PropertyValue)ps) }
                    );
                }
            }
        }

        // Load the sequence JSON (includes rule that triggers sequences)
        SceneLoader.Instance.LoadGameScene("SequenceDriver/Fixtures/sparse-poison-sequence.json");
    }

    [Benchmark]
    public void RunPoisonSimulation()
    {
        // Execute 100 iterations of poison damage
        // Simulates ~1.67 seconds of gameplay (100 frames at 60 FPS)
        // Frame 1: RulesDriver starts sequences, sequences remove #poisoned tags
        // Frames 2-100: RulesDriver no-op, SequenceDriver ticks poison
        for (int i = 0; i < IterationCount; i++)
        {
            SelectorRegistry.Instance.Recalc();
            global::Atomic.Net.MonoGame.Scenes.RulesDriver.Instance.RunFrame(DeltaTime);
            Sequencing.SequenceDriver.Instance.RunFrame(DeltaTime);
        }
    }
}
