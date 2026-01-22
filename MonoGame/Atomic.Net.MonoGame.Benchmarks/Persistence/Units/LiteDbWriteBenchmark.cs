// benchmarker: LiteDB Write Performance - Testing ALL methods with and without memory pooling
using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using LiteDB;
using SystemJsonSerializer = System.Text.Json.JsonSerializer;
using SystemJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using SystemJsonNamingPolicy = System.Text.Json.JsonNamingPolicy;
using SystemUtf8JsonWriter = System.Text.Json.Utf8JsonWriter;

namespace Atomic.Net.MonoGame.Benchmarks.Persistence.Units;

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
public class LiteDbWriteBenchmark
{
    [Params(10, 100, 1000)]
    public int EntityCount { get; set; }
    
    private TestEntity[] _testEntities = [];
    private string _dbPath = "";
    private LiteDatabase? _db;
    private ILiteCollection<BsonDocument>? _collection;
    
    private readonly SystemJsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = SystemJsonNamingPolicy.CamelCase };
    private ArrayBufferWriter<byte> _bufferWriter = new();
    
    [GlobalSetup]
    public void Setup()
    {
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
        _collection!.DeleteAll();
    }
    
    // ========== INDIVIDUAL INSERT METHODS ==========
    
    [Benchmark]
    public int IndividualInsert_JsonSerializer()
    {
        var count = 0;
        foreach (var entity in _testEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            var doc = new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json };
            _collection!.Insert(doc);
            count++;
        }
        return count;
    }
    
    [Benchmark]
    public int IndividualInsert_Utf8JsonWriter_Pooled()
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
            var doc = new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json };
            _collection!.Insert(doc);
            count++;
        }
        return count;
    }
    
    [Benchmark]
    public int IndividualInsert_DirectBson()
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
    
    // ========== BULK INSERT METHODS ==========
    
    [Benchmark(Baseline = true)]
    public int BulkInsert_JsonSerializer()
    {
        var documents = new List<BsonDocument>(EntityCount);
        foreach (var entity in _testEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            documents.Add(new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json });
        }
        return _collection!.InsertBulk(documents);
    }
    
    [Benchmark]
    public int BulkInsert_Utf8JsonWriter_Pooled()
    {
        var documents = new List<BsonDocument>(EntityCount);
        foreach (var entity in _testEntities)
        {
            _bufferWriter.Clear();
            using (var writer = new SystemUtf8JsonWriter(_bufferWriter))
            {
                SystemJsonSerializer.Serialize(writer, entity, _jsonOptions);
            }
            var json = Encoding.UTF8.GetString(_bufferWriter.WrittenSpan);
            documents.Add(new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json });
        }
        return _collection!.InsertBulk(documents);
    }
    
    [Benchmark]
    public int BulkInsert_DirectBson()
    {
        var documents = new List<BsonDocument>(EntityCount);
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
    
    // ========== UPSERT METHODS (for updates) ==========
    
    [Benchmark]
    public int Upsert_JsonSerializer()
    {
        var count = 0;
        foreach (var entity in _testEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            var doc = new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json };
            _collection!.Upsert(doc);
            count++;
        }
        return count;
    }
    
    [Benchmark]
    public int Upsert_Utf8JsonWriter_Pooled()
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
            var doc = new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json };
            _collection!.Upsert(doc);
            count++;
        }
        return count;
    }
    
    [Benchmark]
    public int Upsert_DirectBson()
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
            _collection!.Upsert(doc);
            count++;
        }
        return count;
    }
}
