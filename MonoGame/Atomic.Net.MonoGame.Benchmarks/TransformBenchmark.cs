using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.BED.Hierarchy;
using BenchmarkDotNet. Attributes;
using Microsoft. Xna.Framework;

namespace Atomic.Net.MonoGame.Benchmarks;

[MemoryDiagnoser]
public class TransformBenchmark
{
    // Block size is 16 (SIMD width), MaxEntities = 512, MaxLoadingEntities = 32
    // Scene entities: indices 32-511 = 480 total available
    private const int LowEntityCount = 200;      // 4 blocks - simple scene
    private const int MediumEntityCount = 1000;  // 16 blocks - typical game scene  
    private const int HighEntityCount = 8000;    // 30 blocks - max scene entities (bullet hell/particle storm)

    // Manual computation storage
    private Matrix[] _manualResults = null! ;
    private TransformData[] _transformData = null!;
    private int[] _parentIndices = null!;

    private readonly struct TransformData
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;
        public readonly Vector3 Anchor;

        public TransformData(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Vector3 anchor
        )
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Anchor = anchor;
        }
    }

    [Params(LowEntityCount, MediumEntityCount, HighEntityCount)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        AtomicSystem.Initialize();
        BEDSystem.Initialize();
        TransformSystem.Initialize();
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

        _manualResults = new Matrix[EntityCount];
        _transformData = new TransformData[EntityCount];
        _parentIndices = new int[EntityCount];

        var random = new Random(42);

        for (int i = 0; i < EntityCount; i++)
        {
            // Create branching hierarchy: 
            // - Entity 0: root
            // - Entities 1-8: first level children (8 branches)
            // - Subsequent entities: distributed across branches in layers
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

            _parentIndices[i] = parentIndex;

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

            _transformData[i] = new TransformData(position, rotation, scale, anchor);
        }

        // Setup entities for SIMD benchmark (not measured)
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = EntityRegistry.Instance. Activate();
            var data = _transformData[i];

            entity.WithTransform((ref readonly t) =>
            {
                t.Position. X. Value = data.Position.X;
                t.Position.Y.Value = data.Position.Y;
                t.Position.Z.Value = data.Position.Z;

                t.Rotation.X. Value = data.Rotation.X;
                t.Rotation. Y.Value = data.Rotation.Y;
                t. Rotation.Z.Value = data. Rotation.Z;
                t.Rotation.W. Value = data.Rotation.W;

                t.Scale.X.Value = data.Scale.X;
                t.Scale.Y.Value = data.Scale.Y;
                t.Scale.Z.Value = data.Scale.Z;

                t.Anchor.X. Value = data.Anchor.X;
                t.Anchor. Y.Value = data.Anchor.Y;
                t. Anchor.Z.Value = data. Anchor.Z;
            });

            if (_parentIndices[i] >= 0)
            {
                ushort parentIndex = (ushort)(Constants.MaxLoadingEntities + _parentIndices[i]);
                entity.WithParent(EntityRegistry.Instance[parentIndex]);
            }
        }
    }

    [Benchmark]
    public float Manual_MonoGame()
    {
        float result = 0.0f;
        for (int i = 0; i < EntityCount; i++)
        {
            var data = _transformData[i];

            var localTransform =
                Matrix.CreateTranslation(-data. Anchor) *
                Matrix.CreateScale(data.Scale) *
                Matrix.CreateFromQuaternion(data.Rotation) *
                Matrix.CreateTranslation(data. Anchor) *
                Matrix.CreateTranslation(data.Position);

            if (_parentIndices[i] >= 0)
            {
                _manualResults[i] = localTransform * _manualResults[_parentIndices[i]];
            }
            else
            {
                _manualResults[i] = localTransform;
            }
            result += _manualResults[i].M11;
        }
        return result;
    }

    [Benchmark]
    public void SIMD_EntitySystem()
    {
        TransformRegistry.Instance.Recalculate();
    }
}
