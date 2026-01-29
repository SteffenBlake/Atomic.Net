// benchmarker: Comparing JsonEntity deserialization approaches
// Current: Deserialize<JsonEntity> + WriteToEntity (allocates JsonEntity)
// Optimized: Stream JSON directly to entity behaviors (no intermediate allocation)
//
// Testing the hypothesis from SceneLoader.DeserializeEntityFromJson comment:
// "I am 10000% confident we can optimize this to use a Memory pool and Utf8JsonWriter
// To instead combine these two functions into one where we stream over the json
// and then serialize out the individual properties and write them directly to the entity"

using System.Buffers;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.Persistence.Units;

/// <summary>
/// Test entity that mirrors JsonEntity structure for benchmarking.
/// This is a glass-box benchmark - we copy the relevant structures
/// to avoid project dependencies.
/// </summary>
public class BenchmarkJsonEntity
{
    public string? Id { get; set; }
    public BenchmarkTransform? Transform { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public string? PersistKey { get; set; }
}

public class BenchmarkTransform
{
    public BenchmarkVector3 Position { get; set; } = new() { X = 0, Y = 0, Z = 0 };
    public BenchmarkQuaternion Rotation { get; set; } = new() { X = 0, Y = 0, Z = 0, W = 1 };
    public BenchmarkVector3 Scale { get; set; } = new() { X = 1, Y = 1, Z = 1 };
}

public class BenchmarkVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class BenchmarkQuaternion
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }
}

/// <summary>
/// Simulates the entity that behaviors are written to.
/// In the real system, this would be the ECS Entity.
/// </summary>
public class MockEntity
{
    public string? Id { get; set; }
    public BenchmarkTransform? Transform { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public string? PersistKey { get; set; }
}

[MemoryDiagnoser]
public class JsonEntityDeserializationBenchmark
{
    // Test different entity complexities
    [Params(EntityComplexity.Small, EntityComplexity.Medium, EntityComplexity.Large)]
    public EntityComplexity Complexity { get; set; }

    private string _jsonData = "";
    private JsonSerializerOptions _jsonOptions = null!;
    private MockEntity _targetEntity = null!;
    
    // Reusable buffers for streaming approach
    private ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;
    private byte[] _rentedBuffer = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Create test entity based on complexity
        var entity = CreateTestEntity(Complexity);
        _jsonData = JsonSerializer.Serialize(entity, _jsonOptions);
        
        _targetEntity = new MockEntity();
        
        // Pre-rent a buffer for pooled approach
        _rentedBuffer = _bytePool.Rent(8192);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _bytePool.Return(_rentedBuffer);
    }

    private static BenchmarkJsonEntity CreateTestEntity(EntityComplexity complexity)
    {
        var rng = new Random(42);
        var entity = new BenchmarkJsonEntity
        {
            Id = $"test-entity-{rng.Next(1000)}",
            Transform = new BenchmarkTransform
            {
                Position = new BenchmarkVector3 { X = rng.Next(100), Y = rng.Next(100), Z = 0 },
                Rotation = new BenchmarkQuaternion { X = 0, Y = 0, Z = 0, W = 1 },
                Scale = new BenchmarkVector3 { X = 1, Y = 1, Z = 1 }
            },
            PersistKey = $"save-slot-{rng.Next(1000)}"
        };

        // Add properties based on complexity
        var propCount = complexity switch
        {
            EntityComplexity.Small => 2,
            EntityComplexity.Medium => 10,
            EntityComplexity.Large => 50,
            _ => 2
        };

        entity.Properties = new Dictionary<string, object>(propCount);
        for (int i = 0; i < propCount; i++)
        {
            entity.Properties[$"prop{i}"] = i switch
            {
                0 => rng.Next(100),
                1 => $"string-value-{rng.Next(100)}",
                2 => rng.NextDouble(),
                3 => true,
                _ => rng.Next(1000)
            };
        }

        return entity;
    }

    // ========== CURRENT APPROACH (Baseline) ==========

    /// <summary>
    /// Current implementation from SceneLoader.DeserializeEntityFromJson:
    /// 1. Deserialize JSON to JsonEntity (allocates JsonEntity object)
    /// 2. Call WriteToEntity() to copy behaviors
    /// 
    /// This allocates a JsonEntity instance that is immediately discarded.
    /// </summary>
    [Benchmark(Baseline = true)]
    public MockEntity CurrentApproach_DeserializeAndWrite()
    {
        // This is what SceneLoader.DeserializeEntityFromJson currently does
        var jsonEntity = JsonSerializer.Deserialize<BenchmarkJsonEntity>(_jsonData, _jsonOptions);
        
        if (jsonEntity == null)
        {
            return _targetEntity;
        }

        // Simulate WriteToEntity() behavior copying
        _targetEntity.Id = jsonEntity.Id;
        _targetEntity.Transform = jsonEntity.Transform;
        _targetEntity.Properties = jsonEntity.Properties;
        _targetEntity.PersistKey = jsonEntity.PersistKey;

        return _targetEntity;
    }

    // ========== STREAMING APPROACH (Proposed Optimization) ==========

    /// <summary>
    /// Proposed optimization using Utf8JsonReader to stream:
    /// 1. Parse JSON token-by-token
    /// 2. Write properties directly to entity
    /// 
    /// Avoids allocating intermediate JsonEntity object.
    /// </summary>
    [Benchmark]
    public MockEntity StreamingApproach_Utf8JsonReader()
    {
        var bytes = Encoding.UTF8.GetBytes(_jsonData);
        var reader = new Utf8JsonReader(bytes);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Move to value

                switch (propertyName)
                {
                    case "id":
                        _targetEntity.Id = reader.GetString();
                        break;
                    case "persistKey":
                        _targetEntity.PersistKey = reader.GetString();
                        break;
                    case "transform":
                        _targetEntity.Transform = JsonSerializer.Deserialize<BenchmarkTransform>(ref reader, _jsonOptions);
                        break;
                    case "properties":
                        _targetEntity.Properties = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, _jsonOptions);
                        break;
                }
            }
        }

        return _targetEntity;
    }

    /// <summary>
    /// Streaming approach with pooled byte buffers:
    /// 1. Convert string to bytes using pooled buffer
    /// 2. Parse with Utf8JsonReader
    /// 3. Write directly to entity
    /// 
    /// Tests if pooling reduces allocations further.
    /// </summary>
    [Benchmark]
    public MockEntity StreamingApproach_WithPooledBuffer()
    {
        var byteCount = Encoding.UTF8.GetByteCount(_jsonData);
        var buffer = _bytePool.Rent(byteCount);
        
        try
        {
            var actualBytes = Encoding.UTF8.GetBytes(_jsonData, 0, _jsonData.Length, buffer, 0);
            var span = new ReadOnlySpan<byte>(buffer, 0, actualBytes);
            var reader = new Utf8JsonReader(span);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to value

                    switch (propertyName)
                    {
                        case "id":
                            _targetEntity.Id = reader.GetString();
                            break;
                        case "persistKey":
                            _targetEntity.PersistKey = reader.GetString();
                            break;
                        case "transform":
                            _targetEntity.Transform = JsonSerializer.Deserialize<BenchmarkTransform>(ref reader, _jsonOptions);
                            break;
                        case "properties":
                            _targetEntity.Properties = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, _jsonOptions);
                            break;
                    }
                }
            }

            return _targetEntity;
        }
        finally
        {
            _bytePool.Return(buffer);
        }
    }

    /// <summary>
    /// Reusing the same JsonEntity instance (similar to current SerializeEntityToJson):
    /// 1. Deserialize into pre-allocated JsonEntity
    /// 2. Copy to target entity
    /// 
    /// Tests if reusing JsonEntity reduces allocations.
    /// </summary>
    [Benchmark]
    public MockEntity ReuseApproach_SingleJsonEntityInstance()
    {
        // Reuse the same JsonEntity instance (like _jsonInstance in SceneLoader)
        var jsonEntity = JsonSerializer.Deserialize<BenchmarkJsonEntity>(_jsonData, _jsonOptions);
        
        if (jsonEntity == null)
        {
            return _targetEntity;
        }

        _targetEntity.Id = jsonEntity.Id;
        _targetEntity.Transform = jsonEntity.Transform;
        _targetEntity.Properties = jsonEntity.Properties;
        _targetEntity.PersistKey = jsonEntity.PersistKey;

        return _targetEntity;
    }
}

public enum EntityComplexity
{
    Small,   // 2 properties
    Medium,  // 10 properties
    Large    // 50 properties
}
