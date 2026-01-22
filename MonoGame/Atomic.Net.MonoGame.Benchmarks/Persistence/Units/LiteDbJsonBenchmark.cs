// benchmarker: This benchmark tests different approaches for reading/writing JSON to/from LiteDB
// The goal is to find the fastest method for entity persistence, with focus on zero-alloc writes

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using LiteDB;
using SystemJsonSerializer = System.Text.Json.JsonSerializer;
using SystemJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using SystemJsonNamingPolicy = System.Text.Json.JsonNamingPolicy;
using SystemUtf8JsonWriter = System.Text.Json.Utf8JsonWriter;
using SystemUtf8JsonReader = System.Text.Json.Utf8JsonReader;

namespace Atomic.Net.MonoGame.Benchmarks.Persistence.Units;

// Test entity that represents a typical game entity with multiple behaviors
public class TestEntity
{
    public string? Id { get; set; }
    public TransformData? Transform { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public string? PersistKey { get; set; }
}

public class TransformData
{
    public float[] Position { get; set; } = [0, 0, 0];
    public float[] Rotation { get; set; } = [0, 0, 0, 1];
    public float[] Scale { get; set; } = [1, 1, 1];
}

[MemoryDiagnoser]
public class LiteDbJsonWriteBenchmark
{
    [Params(10, 100, 1000)]
    public int EntityCount { get; set; }
    
    private TestEntity[] _testEntities = [];
    private string _dbPath = "";
    private LiteDatabase? _db;
    private ILiteCollection<BsonDocument>? _collection;
    
    // Pooled resources for zero-alloc approaches
    private ArrayBufferWriter<byte> _bufferWriter = new();
    private readonly SystemJsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = SystemJsonNamingPolicy.CamelCase };
    
    [GlobalSetup]
    public void Setup()
    {
        // Generate test entities with realistic data
        _testEntities = new TestEntity[EntityCount];
        var rng = new Random(42);
        
        for (int i = 0; i < EntityCount; i++)
        {
            _testEntities[i] = new TestEntity
            {
                Id = $"entity-{i}",
                Transform = new TransformData
                {
                    Position = [rng.Next(100), rng.Next(100), 0],
                    Rotation = [0, 0, 0, 1],
                    Scale = [1, 1, 1]
                },
                Properties = new Dictionary<string, object>
                {
                    ["health"] = rng.Next(1, 100),
                    ["mana"] = rng.Next(1, 100),
                    ["level"] = rng.Next(1, 50),
                    ["name"] = $"Character {i}"
                },
                PersistKey = $"save-slot-{i}"
            };
        }
        
        // Setup database
        _dbPath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
        _collection = _db.GetCollection("entities");
        _collection.EnsureIndex("_id");
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _db?.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        // Clear collection between iterations
        _collection!.DeleteAll();
    }
    
    // Baseline: JsonSerializer.Serialize to string, then LiteDB
    [Benchmark(Baseline = true)]
    public int ApproachA_JsonSerializer_String()
    {
        var count = 0;
        foreach (var entity in _testEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            var doc = new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = json
            };
            _collection!.Insert(doc);
            count++;
        }
        return count;
    }
    
    // Approach B: Utf8JsonWriter with ArrayBufferWriter (reusable buffer)
    [Benchmark]
    public int ApproachB_Utf8JsonWriter_Pooled()
    {
        var count = 0;
        foreach (var entity in _testEntities)
        {
            _bufferWriter.Clear();
            using (var writer = new SystemUtf8JsonWriter(_bufferWriter))
            {
                SystemJsonSerializer.Serialize(writer, entity, _jsonOptions);
            }
            
            var json = Encoding.UTF8.GetString(_bufferWriter.WrittenSpan);
            var doc = new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = json
            };
            _collection!.Insert(doc);
            count++;
        }
        return count;
    }
    
    // Approach C: Direct BsonDocument (no JSON string intermediate)
    [Benchmark]
    public int ApproachC_DirectBson()
    {
        var count = 0;
        foreach (var entity in _testEntities)
        {
            var doc = new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["id"] = entity.Id,
                ["transform"] = new BsonDocument
                {
                    ["position"] = new BsonArray(entity.Transform!.Position.Select(x => new BsonValue(x))),
                    ["rotation"] = new BsonArray(entity.Transform.Rotation.Select(x => new BsonValue(x))),
                    ["scale"] = new BsonArray(entity.Transform.Scale.Select(x => new BsonValue(x)))
                },
                ["properties"] = new BsonDocument(entity.Properties!.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new BsonValue(kvp.Value)
                ))
            };
            _collection!.Insert(doc);
            count++;
        }
        return count;
    }
    
    // Approach D: Batch insert with transaction
    [Benchmark]
    public int ApproachD_BatchInsert()
    {
        var documents = new List<BsonDocument>(_testEntities.Length);
        
        foreach (var entity in _testEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            documents.Add(new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = json
            });
        }
        
        return _collection!.InsertBulk(documents);
    }
    
    // Approach E: BulkInsert with direct BSON (best of both worlds?)
    [Benchmark]
    public int ApproachE_BulkInsert_DirectBson()
    {
        var documents = new List<BsonDocument>(_testEntities.Length);
        
        foreach (var entity in _testEntities)
        {
            documents.Add(new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["id"] = entity.Id,
                ["transform"] = new BsonDocument
                {
                    ["position"] = new BsonArray(entity.Transform!.Position.Select(x => new BsonValue(x))),
                    ["rotation"] = new BsonArray(entity.Transform.Rotation.Select(x => new BsonValue(x))),
                    ["scale"] = new BsonArray(entity.Transform.Scale.Select(x => new BsonValue(x)))
                },
                ["properties"] = new BsonDocument(entity.Properties!.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new BsonValue(kvp.Value)
                ))
            });
        }
        
        return _collection!.InsertBulk(documents);
    }
}

[MemoryDiagnoser]
public class LiteDbJsonReadBenchmark
{
    [Params(10, 100, 1000)]
    public int EntityCount { get; set; }
    
    private string _dbPath = "";
    private LiteDatabase? _db;
    private ILiteCollection<BsonDocument>? _collection;
    
    private readonly SystemJsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = SystemJsonNamingPolicy.CamelCase };
    
    [GlobalSetup]
    public void Setup()
    {
        // Setup database with test data
        _dbPath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
        _collection = _db.GetCollection("entities");
        _collection.EnsureIndex("_id");
        
        // Populate database with test entities
        var rng = new Random(42);
        var documents = new List<BsonDocument>(EntityCount);
        
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = new TestEntity
            {
                Id = $"entity-{i}",
                Transform = new TransformData
                {
                    Position = [rng.Next(100), rng.Next(100), 0],
                    Rotation = [0, 0, 0, 1],
                    Scale = [1, 1, 1]
                },
                Properties = new Dictionary<string, object>
                {
                    ["health"] = rng.Next(1, 100),
                    ["mana"] = rng.Next(1, 100),
                    ["level"] = rng.Next(1, 50),
                    ["name"] = $"Character {i}"
                },
                PersistKey = $"save-slot-{i}"
            };
            
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            documents.Add(new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = json
            });
        }
        
        _collection.InsertBulk(documents);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _db?.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
    
    // Baseline: Read all, deserialize from JSON string
    [Benchmark(Baseline = true)]
    public int ApproachA_JsonDeserialize_String()
    {
        var count = 0;
        var documents = _collection!.FindAll();
        
        foreach (var doc in documents)
        {
            var json = doc["data"].AsString;
            var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
            if (entity != null) count++;
        }
        
        return count;
    }
    
    // Approach B: Read with filter, deserialize
    [Benchmark]
    public int ApproachB_FilteredRead()
    {
        var count = 0;
        
        for (int i = 0; i < EntityCount; i++)
        {
            var doc = _collection!.FindById($"save-slot-{i}");
            if (doc != null)
            {
                var json = doc["data"].AsString;
                var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
                if (entity != null) count++;
            }
        }
        
        return count;
    }
    
    // Approach C: Utf8JsonReader with ReadOnlySpan
    [Benchmark]
    public int ApproachC_Utf8JsonReader()
    {
        var count = 0;
        var documents = _collection!.FindAll();
        
        foreach (var doc in documents)
        {
            var json = doc["data"].AsString;
            var bytes = Encoding.UTF8.GetBytes(json);
            var reader = new SystemUtf8JsonReader(bytes);
            
            // Simplified parsing - just validate it's readable
            while (reader.Read())
            {
                // Process tokens
            }
            count++;
        }
        
        return count;
    }
    
    // Approach D: Direct BSON access (if data was stored as BSON)
    // Note: This would require changing how we write data
    [Benchmark]
    public int ApproachD_DirectBsonRead()
    {
        var count = 0;
        var documents = _collection!.FindAll();
        
        foreach (var doc in documents)
        {
            // Simulate reading BSON fields directly
            var id = doc["_id"].AsString;
            count++;
        }
        
        return count;
    }
}
