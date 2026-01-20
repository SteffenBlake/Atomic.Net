using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Transform;
using Microsoft.Xna.Framework;
using Xunit;

namespace Atomic.Net.MonoGame.Tests.Transform;

[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class TransformBenchmarkTests : IDisposable
{
    private const float Tolerance = 0.001f;
    private readonly List<Entity> _entities = [];

    public TransformBenchmarkTests()
    {
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        TransformSystem.Initialize();

        // Trigger initialization to register event handlers
        EventBus<InitializeEvent>.Push(new());
    }

    private Entity CreateEntity()
    {
        var entity = EntityRegistry.Instance.Activate();
        _entities.Add(entity);
        return entity;
    }

    public void Dispose()
    {
        // Fire reset event to clean up scene entities between tests
        EventBus<ResetEvent>.Push(new());
        _entities.Clear();
    }

    [Fact]
    public void Manual_vs_SIMD_ProducesSameResults()
    {
        const int testEntityCount = 64;

        var random = new Random(42);
        var manualResults = new Matrix[testEntityCount];
        var transformData = new TransformData[testEntityCount];
        var parentIndices = new int[testEntityCount];

        // Generate test data (same logic as benchmark)
        for (int i = 0; i < testEntityCount; i++)
        {
            int parentIndex;
            if (i == 0)
            {
                parentIndex = -1;
            }
            else if (i <= 8)
            {
                parentIndex = 0;
            }
            else
            {
                int branch = (i - 9) % 8 + 1;
                int depth = (i - 9) / 8;
                parentIndex = branch + (depth * 8);

                if (parentIndex >= i)
                {
                    parentIndex = branch;
                }
            }

            parentIndices[i] = parentIndex;

            var position = new Vector3(
                (float)(random.NextDouble() * 100 - 50),
                (float)(random.NextDouble() * 100 - 50),
                (float)(random.NextDouble() * 100 - 50)
            );

            var rotation = Quaternion.CreateFromAxisAngle(
                Vector3.Normalize(new Vector3(
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble()
                )),
                (float)(random.NextDouble() * MathHelper.TwoPi)
            );

            var scale = new Vector3(
                (float)(random.NextDouble() * 2 + 0.5f),
                (float)(random.NextDouble() * 2 + 0.5f),
                (float)(random.NextDouble() * 2 + 0.5f)
            );

            var anchor = new Vector3(
                (float)(random.NextDouble() * 10 - 5),
                (float)(random.NextDouble() * 10 - 5),
                (float)(random.NextDouble() * 10 - 5)
            );

            transformData[i] = new TransformData(position, rotation, scale, anchor);
        }

        // Step 1: Manual MonoGame calculation
        for (int i = 0; i < testEntityCount; i++)
        {
            var data = transformData[i];

            var localTransform =
                Matrix.CreateTranslation(-data.Anchor) *
                Matrix.CreateScale(data.Scale) *
                Matrix.CreateFromQuaternion(data.Rotation) *
                Matrix.CreateTranslation(data.Anchor) *
                Matrix.CreateTranslation(data.Position);

            if (parentIndices[i] >= 0)
            {
                manualResults[i] = localTransform * manualResults[parentIndices[i]];
            }
            else
            {
                manualResults[i] = localTransform;
            }
        }

        // Step 2: SIMD EntitySystem calculation
        for (int i = 0; i < testEntityCount; i++)
        {
            var entity = EntityRegistry.Instance.Activate();
            var data = transformData[i];

            entity.WithTransform((ref t) =>
            {
                t.Position = data.Position;
                t.Rotation = data.Rotation;
                t.Scale = data.Scale;
                t.Anchor = data.Anchor;
            });

            if (parentIndices[i] >= 0)
            {
                ushort parentIndex = (ushort)(Constants.MaxLoadingEntities + parentIndices[i]);
                entity.WithParent(EntityRegistry.Instance[parentIndex]);
            }
        }

        TransformRegistry.Instance.Recalculate();

        // Step 3: Compare results
        for (int i = 0; i < testEntityCount; i++)
        {
            var entityIndex = (ushort)(Constants.MaxLoadingEntities + i);
            var entity = EntityRegistry.Instance[entityIndex];

            var hasTransform = BehaviorRegistry<WorldTransformBehavior>.Instance
                .TryGetBehavior(entity, out var worldTransform);

            Assert.True(hasTransform);

            var simdMatrix = worldTransform!.Value.Value;
            var manualMatrix = manualResults[i];

            AssertMatricesEqual(manualMatrix, simdMatrix, Tolerance);
        }
    }

    private static void AssertMatricesEqual(Matrix expected, Matrix actual, float tolerance)
    {
        var expectedArray = new float[]
        {
        expected.M11, expected.M12, expected.M13, expected.M14,
        expected.M21, expected.M22, expected.M23, expected.M24,
        expected.M31, expected. M32, expected.M33, expected.M34,
        expected.M41, expected.M42, expected.M43, expected. M44
        };

        var actualArray = new float[]
        {
        actual.M11, actual.M12, actual.M13, actual.M14,
        actual.M21, actual.M22, actual.M23, actual.M24,
        actual.M31, actual. M32, actual.M33, actual.M34,
        actual.M41, actual.M42, actual.M43, actual. M44
        };

        for (int i = 0; i < 16; i++)
        {
            Assert.True(
                MathF.Abs(expectedArray[i] - actualArray[i]) <= tolerance,
                $"Matrix element {i}:  expected {expectedArray[i]}, actual {actualArray[i]}"
            );
        }
    }

    private readonly record struct TransformData(
        Vector3 Position, Quaternion Rotation, Vector3 Scale, Vector3 Anchor
    );

#if BENCH_TRANSFORM
    const string? BenchTransform = null;
#else
    const string? BenchTransform = "Performance profiling only - run via -p:DefineConstants=BENCH_TRANSFORM";
#endif

    private const int EntityCount = 8000;

    [Fact(Skip = BenchTransform)]
    public void ProfileTransformRecalculation()
    {
        var random = new Random(42);

        // Setup hierarchy with 8000 entities
        for (int i = 0; i < EntityCount; i++)
        {
            int parentIndex = i == 0 ? -1 
                : i <= 8 ? 0 
                : (i - 9) % 8 + 1;

            var entity = EntityRegistry.Instance. Activate();
            
            var rotation = Quaternion.CreateFromAxisAngle(
                Vector3.Normalize(new Vector3(
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble()
                )),
                (float)(random.NextDouble() * MathHelper.TwoPi)
            );

            entity.WithTransform((ref t) =>
            {
                t.Position = new Vector3(
                    (float)(random.NextDouble() * 100),
                    (float)(random.NextDouble() * 100),
                    (float)(random.NextDouble() * 100));
                
                t.Rotation = rotation;
                
                t.Scale = new Vector3(
                    (float)(random.NextDouble() * 2 + 0.5f),
                    (float)(random.NextDouble() * 2 + 0.5f),
                    (float)(random.NextDouble() * 2 + 0.5f));
                
                t.Anchor = Vector3.Zero;
            });

            if (parentIndex >= 0)
            {
                ushort parentEntityIndex = (ushort)(Constants.MaxLoadingEntities + parentIndex);
                entity.WithParent(EntityRegistry.Instance[parentEntityIndex]);
            }
        }

        // THE HOTSPOT: Run multiple iterations to get good profiling data
        for (int iteration = 0; iteration < 100; iteration++)
        {
            TransformRegistry.Instance.Recalculate();
        }
    }
}

