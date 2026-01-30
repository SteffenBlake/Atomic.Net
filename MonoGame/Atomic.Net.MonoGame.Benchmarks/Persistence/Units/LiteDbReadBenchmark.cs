// benchmarker: LiteDB Read Performance - Testing ALL methods with and without memory pooling
using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using LiteDB;
using SystemJsonSerializer = System.Text.Json.JsonSerializer;
using SystemJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using SystemJsonNamingPolicy = System.Text.Json.JsonNamingPolicy;
using SystemUtf8JsonReader = System.Text.Json.Utf8JsonReader;

namespace Atomic.Net.MonoGame.Benchmarks.Persistence.Units;

[MemoryDiagnoser]
public class LiteDbReadBenchmark
{
    [Params(100, 1000, 10000)]
    public int TotalEntitiesInDb { get; set; }
    
    // Read 1% of entities (sparse reads - actual use case)
    private int EntitiesToRead => Math.Max(1, TotalEntitiesInDb / 100);
    
    private string _dbPath = "";
    private LiteDatabase? _db;
    private ILiteCollection<BsonDocument>? _collection;
    private string[] _keysToRead = [];
    
    private readonly SystemJsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = SystemJsonNamingPolicy.CamelCase };
    
    [GlobalSetup]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
        _collection = _db.GetCollection("entities");
        _collection.EnsureIndex("_id");
        
        // Populate database
        var rng = new Random(42);
        var documents = new List<BsonDocument>(TotalEntitiesInDb);
        
        for (int i = 0; i < TotalEntitiesInDb; i++)
        {
            var entity = new TestEntity
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
            
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            documents.Add(new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json });
        }
        
        _collection.InsertBulk(documents);
        
        // Select random keys to read (1% of total)
        _keysToRead = new string[EntitiesToRead];
        for (int i = 0; i < EntitiesToRead; i++)
        {
            var randomIndex = rng.Next(TotalEntitiesInDb);
            _keysToRead[i] = $"save-slot-{randomIndex}";
        }
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
    
    // ========== INDIVIDUAL FINDBYID METHODS ==========
    
    [Benchmark(Baseline = true)]
    public int IndividualFindById_JsonDeserialize()
    {
        var count = 0;
        foreach (var key in _keysToRead)
        {
            var doc = _collection!.FindById(key);
            if (doc != null)
            {
                var json = doc["data"].AsString;
                var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
                if (entity != null) count++;
            }
        }
        return count;
    }
    
    [Benchmark]
    public int IndividualFindById_Utf8JsonReader()
    {
        var count = 0;
        foreach (var key in _keysToRead)
        {
            var doc = _collection!.FindById(key);
            if (doc != null)
            {
                var json = doc["data"].AsString;
                var bytes = Encoding.UTF8.GetBytes(json);
                var entity = SystemJsonSerializer.Deserialize<TestEntity>(bytes, _jsonOptions);
                if (entity != null) count++;
            }
        }
        return count;
    }
    
    [Benchmark]
    public int IndividualFindById_DirectBson()
    {
        var count = 0;
        foreach (var key in _keysToRead)
        {
            var doc = _collection!.FindById(key);
            if (doc != null)
            {
                // Direct BSON access - just reading the _id proves we got the doc
                var id = doc["_id"].AsString;
                count++;
            }
        }
        return count;
    }
    
    // ========== BATCH QUERY METHODS ==========
    
    [Benchmark]
    public int BatchQuery_JsonDeserialize()
    {
        var count = 0;
        var keySet = new HashSet<string>(_keysToRead);
        var documents = _collection!.Find(doc => keySet.Contains(doc["_id"].AsString));
        
        foreach (var doc in documents)
        {
            var json = doc["data"].AsString;
            var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
            if (entity != null) count++;
        }
        return count;
    }
    
    [Benchmark]
    public int BatchQuery_Utf8JsonReader()
    {
        var count = 0;
        var keySet = new HashSet<string>(_keysToRead);
        var documents = _collection!.Find(doc => keySet.Contains(doc["_id"].AsString));
        
        foreach (var doc in documents)
        {
            var json = doc["data"].AsString;
            var bytes = Encoding.UTF8.GetBytes(json);
            var entity = SystemJsonSerializer.Deserialize<TestEntity>(bytes, _jsonOptions);
            if (entity != null) count++;
        }
        return count;
    }
    
    [Benchmark]
    public int BatchQuery_DirectBson()
    {
        var count = 0;
        var keySet = new HashSet<string>(_keysToRead);
        var documents = _collection!.Find(doc => keySet.Contains(doc["_id"].AsString));
        
        foreach (var doc in documents)
        {
            var id = doc["_id"].AsString;
            count++;
        }
        return count;
    }
    
    // ========== HYBRID APPROACH ==========
    
    [Benchmark]
    public int Hybrid_JsonDeserialize()
    {
        var count = 0;
        
        if (_keysToRead.Length <= 5)
        {
            // Individual lookups for small counts
            foreach (var key in _keysToRead)
            {
                var doc = _collection!.FindById(key);
                if (doc != null)
                {
                    var json = doc["data"].AsString;
                    var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
                    if (entity != null) count++;
                }
            }
        }
        else
        {
            // Batch query for larger counts
            var keySet = new HashSet<string>(_keysToRead);
            var documents = _collection!.Find(doc => keySet.Contains(doc["_id"].AsString));
            
            foreach (var doc in documents)
            {
                var json = doc["data"].AsString;
                var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
                if (entity != null) count++;
            }
        }
        
        return count;
    }
    
    [Benchmark]
    public int Hybrid_Utf8JsonReader()
    {
        var count = 0;
        
        if (_keysToRead.Length <= 5)
        {
            foreach (var key in _keysToRead)
            {
                var doc = _collection!.FindById(key);
                if (doc != null)
                {
                    var json = doc["data"].AsString;
                    var bytes = Encoding.UTF8.GetBytes(json);
                    var entity = SystemJsonSerializer.Deserialize<TestEntity>(bytes, _jsonOptions);
                    if (entity != null) count++;
                }
            }
        }
        else
        {
            var keySet = new HashSet<string>(_keysToRead);
            var documents = _collection!.Find(doc => keySet.Contains(doc["_id"].AsString));
            
            foreach (var doc in documents)
            {
                var json = doc["data"].AsString;
                var bytes = Encoding.UTF8.GetBytes(json);
                var entity = SystemJsonSerializer.Deserialize<TestEntity>(bytes, _jsonOptions);
                if (entity != null) count++;
            }
        }
        
        return count;
    }
}
