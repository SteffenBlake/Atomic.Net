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
    public Vector3Data Position { get; set; } = new() { X = 0, Y = 0, Z = 0 };
    public QuaternionData Rotation { get; set; } = new() { X = 0, Y = 0, Z = 0, W = 1 };
    public Vector3Data Scale { get; set; } = new() { X = 1, Y = 1, Z = 1 };
}

public class Vector3Data
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class QuaternionData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }
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
    private readonly ArrayBufferWriter<byte> _bufferWriter = new(1000);

    private readonly List<BsonDocument> _documentsPreAllocd = new(1000);

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
                    Position = new Vector3Data { X = rng.Next(100), Y = rng.Next(100), Z = 0 },
                    Rotation = new QuaternionData { X = 0, Y = 0, Z = 0, W = 1 },
                    Scale = new Vector3Data { X = 1, Y = 1, Z = 1 }
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
        using var writer = new SystemUtf8JsonWriter(_bufferWriter);

        foreach (var entity in _testEntities)
        {
            SystemJsonSerializer.Serialize(writer, entity, _jsonOptions);
            var doc = new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = Encoding.UTF8.GetString(_bufferWriter.WrittenSpan)
            };

            _collection!.Insert(doc);
            count++;

            _bufferWriter.Clear();
            writer.Reset();
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
                    ["position"] = new BsonDocument
                    {
                        ["x"] = entity.Transform!.Position.X,
                        ["y"] = entity.Transform.Position.Y,
                        ["z"] = entity.Transform.Position.Z
                    },
                    ["rotation"] = new BsonDocument
                    {
                        ["x"] = entity.Transform.Rotation.X,
                        ["y"] = entity.Transform.Rotation.Y,
                        ["z"] = entity.Transform.Rotation.Z,
                        ["w"] = entity.Transform.Rotation.W
                    },
                    ["scale"] = new BsonDocument
                    {
                        ["x"] = entity.Transform.Scale.X,
                        ["y"] = entity.Transform.Scale.Y,
                        ["z"] = entity.Transform.Scale.Z
                    }
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
    public int BulkInsert_JsonSerializer_PreAllocd()
    {
        foreach (var entity in _testEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            _documentsPreAllocd.Add(new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json });
        }
        var result = _collection!.InsertBulk(_documentsPreAllocd);
        _documentsPreAllocd.Clear();
        return result;
    }


    [Benchmark]
    public int BulkInsert_Utf8JsonWriter_Pooled()
    {
        using var writer = new SystemUtf8JsonWriter(_bufferWriter);
        var documents = new List<BsonDocument>(EntityCount);
        foreach (var entity in _testEntities)
        {
            SystemJsonSerializer.Serialize(writer, entity, _jsonOptions);
            documents.Add(new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = Encoding.UTF8.GetString(_bufferWriter.WrittenSpan)
            });

            _bufferWriter.Clear();
            writer.Reset();
        }
        return _collection!.InsertBulk(documents);
    }


    [Benchmark]
    public int BulkInsert_Utf8JsonWriter_Pooled_PreAllocd()
    {
        using var writer = new SystemUtf8JsonWriter(_bufferWriter);
        foreach (var entity in _testEntities)
        {
            SystemJsonSerializer.Serialize(writer, entity, _jsonOptions);
            _documentsPreAllocd.Add(new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = Encoding.UTF8.GetString(_bufferWriter.WrittenSpan)
            });
            _bufferWriter.Clear();
            writer.Reset();
        }
        var result = _collection!.InsertBulk(_documentsPreAllocd);
        _documentsPreAllocd.Clear();
        return result;
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
                    ["position"] = new BsonDocument
                    {
                        ["x"] = entity.Transform!.Position.X,
                        ["y"] = entity.Transform.Position.Y,
                        ["z"] = entity.Transform.Position.Z
                    },
                    ["rotation"] = new BsonDocument
                    {
                        ["x"] = entity.Transform.Rotation.X,
                        ["y"] = entity.Transform.Rotation.Y,
                        ["z"] = entity.Transform.Rotation.Z,
                        ["w"] = entity.Transform.Rotation.W
                    },
                    ["scale"] = new BsonDocument
                    {
                        ["x"] = entity.Transform.Scale.X,
                        ["y"] = entity.Transform.Scale.Y,
                        ["z"] = entity.Transform.Scale.Z
                    }
                },
                ["properties"] = new BsonDocument(entity.Properties!.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new BsonValue(kvp.Value)
                ))
            });
        }
        return _collection!.InsertBulk(documents);
    }


    [Benchmark]
    public int BulkInsert_DirectBson_PreAllocd()
    {
        foreach (var entity in _testEntities)
        {
            _documentsPreAllocd.Add(new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["id"] = entity.Id,
                ["transform"] = new BsonDocument
                {
                    ["position"] = new BsonDocument
                    {
                        ["x"] = entity.Transform!.Position.X,
                        ["y"] = entity.Transform.Position.Y,
                        ["z"] = entity.Transform.Position.Z
                    },
                    ["rotation"] = new BsonDocument
                    {
                        ["x"] = entity.Transform.Rotation.X,
                        ["y"] = entity.Transform.Rotation.Y,
                        ["z"] = entity.Transform.Rotation.Z,
                        ["w"] = entity.Transform.Rotation.W
                    },
                    ["scale"] = new BsonDocument
                    {
                        ["x"] = entity.Transform.Scale.X,
                        ["y"] = entity.Transform.Scale.Y,
                        ["z"] = entity.Transform.Scale.Z
                    }
                },
                ["properties"] = new BsonDocument(entity.Properties!.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new BsonValue(kvp.Value)
                ))
            });
        }
        var result = _collection!.InsertBulk(_documentsPreAllocd);
        _documentsPreAllocd.Clear();
        return result;
    }

    // ========== UPSERT METHODS (for updates) ==========

    [Benchmark]
    public int Upsert_JsonSerializer()
    {
        var count = 0;
        foreach (var entity in _testEntities)
        {
            var doc = new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = SystemJsonSerializer.Serialize(entity, _jsonOptions)
            };
            _collection!.Upsert(doc);
            count++;
        }
        return count;
    }

    [Benchmark]
    public int Upsert_Utf8JsonWriter_Pooled()
    {
        var count = 0;
        using var writer = new SystemUtf8JsonWriter(_bufferWriter);
        foreach (var entity in _testEntities)
        {
            var doc = new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = Encoding.UTF8.GetString(_bufferWriter.WrittenSpan)
            };
            _collection!.Upsert(doc);
            count++;
            _bufferWriter.Clear();
            writer.Reset();
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
                    ["position"] = new BsonDocument
                    {
                        ["x"] = entity.Transform!.Position.X,
                        ["y"] = entity.Transform.Position.Y,
                        ["z"] = entity.Transform.Position.Z
                    },
                    ["rotation"] = new BsonDocument
                    {
                        ["x"] = entity.Transform.Rotation.X,
                        ["y"] = entity.Transform.Rotation.Y,
                        ["z"] = entity.Transform.Rotation.Z,
                        ["w"] = entity.Transform.Rotation.W
                    },
                    ["scale"] = new BsonDocument
                    {
                        ["x"] = entity.Transform.Scale.X,
                        ["y"] = entity.Transform.Scale.Y,
                        ["z"] = entity.Transform.Scale.Z
                    }
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
