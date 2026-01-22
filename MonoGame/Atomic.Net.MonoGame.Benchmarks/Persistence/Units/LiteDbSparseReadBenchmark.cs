// benchmarker: CORRECTED BENCHMARK - Sparse entity lookup (only 1% of entities loaded)
// This reflects the ACTUAL use case: looking up specific entities by key, not bulk reads

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using LiteDB;
using SystemJsonSerializer = System.Text.Json.JsonSerializer;
using SystemJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using SystemJsonNamingPolicy = System.Text.Json.JsonNamingPolicy;

namespace Atomic.Net.MonoGame.Benchmarks.Persistence.Units;

/// <summary>
/// benchmarker: ACTUAL USE CASE - Sparse entity lookups
/// Real world: 1000 entities in DB, but only ~10 (1%) need to be loaded
/// THIS is what DatabaseRegistry will do when entities with PersistToDiskBehavior are created
/// </summary>
[MemoryDiagnoser]
public class LiteDbSparseReadBenchmark
{
    [Params(100, 1000, 10000)]
    public int TotalEntitiesInDb { get; set; }
    
    // Only 1% of entities are actually loaded
    private int EntitiesToRead => Math.Max(1, TotalEntitiesInDb / 100);
    
    private string _dbPath = "";
    private LiteDatabase? _db;
    private ILiteCollection<BsonDocument>? _collection;
    private string[] _keysToRead = [];
    
    private readonly SystemJsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNamingPolicy = SystemJsonNamingPolicy.CamelCase 
    };
    
    [GlobalSetup]
    public void Setup()
    {
        // Setup database with many entities
        _dbPath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
        _collection = _db.GetCollection("entities");
        _collection.EnsureIndex("_id");
        
        // Populate database with realistic data
        var rng = new Random(42);
        var documents = new List<BsonDocument>(TotalEntitiesInDb);
        
        for (int i = 0; i < TotalEntitiesInDb; i++)
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
    
    /// <summary>
    /// benchmarker: BASELINE - Individual FindById calls (current naive approach)
    /// This is what happens when entities are created with PersistToDiskBehavior
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ApproachA_IndividualFindById()
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
    
    /// <summary>
    /// benchmarker: OPTIMIZATION - Batch read with Where clause
    /// Fetch all needed entities in one query, then deserialize
    /// </summary>
    [Benchmark]
    public int ApproachB_BatchReadWithWhere()
    {
        var count = 0;
        
        // LiteDB doesn't have IN operator, so we use Contains on array
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
    
    /// <summary>
    /// benchmarker: ALTERNATIVE - Query with manual filter
    /// Build OR query for specific keys
    /// </summary>
    [Benchmark]
    public int ApproachC_QueryWithManualFilter()
    {
        var count = 0;
        
        // Build a Query.Or expression for all keys
        if (_keysToRead.Length == 0) return 0;
        
        var query = Query.EQ("_id", _keysToRead[0]);
        for (int i = 1; i < _keysToRead.Length; i++)
        {
            query = Query.Or(query, Query.EQ("_id", _keysToRead[i]));
        }
        
        var documents = _collection!.Find(query);
        
        foreach (var doc in documents)
        {
            var json = doc["data"].AsString;
            var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
            if (entity != null) count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// benchmarker: HYBRID - Check count, use appropriate strategy
    /// For small counts use FindById, for larger counts use batch
    /// </summary>
    [Benchmark]
    public int ApproachD_Hybrid_SmartSwitch()
    {
        var count = 0;
        
        // Switch strategy based on how many entities to load
        if (_keysToRead.Length <= 5)
        {
            // Few entities - individual lookups are fine
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
            // Many entities - batch query
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
}

/// <summary>
/// benchmarker: WRITE PERFORMANCE - Single entity writes (real use case for Flush())
/// When DatabaseRegistry.Flush() is called, it writes only dirty entities (sparse)
/// </summary>
[MemoryDiagnoser]
public class LiteDbSparseWriteBenchmark
{
    [Params(100, 1000, 10000)]
    public int TotalEntitiesInDb { get; set; }
    
    // Only 1-5% of entities are dirty per frame
    private int DirtyEntitiesToWrite => Math.Max(1, TotalEntitiesInDb / 50); // 2%
    
    private TestEntity[] _dirtyEntities = [];
    private string _dbPath = "";
    private LiteDatabase? _db;
    private ILiteCollection<BsonDocument>? _collection;
    
    private readonly SystemJsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNamingPolicy = SystemJsonNamingPolicy.CamelCase 
    };
    
    [GlobalSetup]
    public void Setup()
    {
        // Generate dirty entities to write
        _dirtyEntities = new TestEntity[DirtyEntitiesToWrite];
        var rng = new Random(42);
        
        for (int i = 0; i < DirtyEntitiesToWrite; i++)
        {
            _dirtyEntities[i] = new TestEntity
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
        
        // Pre-populate DB with existing data to simulate realistic scenario
        var documents = new List<BsonDocument>(TotalEntitiesInDb);
        for (int i = 0; i < TotalEntitiesInDb; i++)
        {
            var entity = new TestEntity
            {
                Id = $"entity-{i}",
                Transform = new TransformData { Position = [0, 0, 0], Rotation = [0, 0, 0, 1], Scale = [1, 1, 1] },
                Properties = new Dictionary<string, object> { ["health"] = 100 },
                PersistKey = $"save-slot-{i}"
            };
            
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            documents.Add(new BsonDocument { ["_id"] = entity.PersistKey, ["data"] = json });
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
    
    [IterationSetup]
    public void IterationSetup()
    {
        // No need to delete - we're updating existing records
    }
    
    /// <summary>
    /// benchmarker: RECOMMENDED - Batch update dirty entities (Upsert)
    /// This is what DatabaseRegistry.Flush() should do
    /// </summary>
    [Benchmark(Baseline = true)]
    public int BatchUpdate_Upsert()
    {
        var count = 0;
        
        foreach (var entity in _dirtyEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            var doc = new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = json
            };
            
            // Upsert: insert if new, update if exists
            _collection!.Upsert(doc);
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// benchmarker: ALTERNATIVE - Check existence then update/insert
    /// More explicit but potentially slower
    /// </summary>
    [Benchmark]
    public int CheckThenUpdate()
    {
        var count = 0;
        
        foreach (var entity in _dirtyEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            var doc = new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = json
            };
            
            var existing = _collection!.FindById(entity.PersistKey);
            if (existing != null)
            {
                _collection.Update(doc);
            }
            else
            {
                _collection.Insert(doc);
            }
            count++;
        }
        
        return count;
    }
}
