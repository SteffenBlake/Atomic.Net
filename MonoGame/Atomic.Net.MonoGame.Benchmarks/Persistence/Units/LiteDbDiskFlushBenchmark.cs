// benchmarker: CRITICAL TEST - Verify LiteDB actually writes to disk
// This benchmark confirms whether InsertBulk() flushes to disk or just buffers in memory

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using LiteDB;
using SystemJsonSerializer = System.Text.Json.JsonSerializer;
using SystemJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using SystemJsonNamingPolicy = System.Text.Json.JsonNamingPolicy;

namespace Atomic.Net.MonoGame.Benchmarks.Persistence.Units;

/// <summary>
/// benchmarker: Tests if LiteDB actually writes to disk or just buffers in memory
/// CRITICAL: If Checkpoint() adds significant overhead, previous benchmarks are misleading
/// </summary>
[MemoryDiagnoser]
public class LiteDbDiskFlushVerificationBenchmark
{
    [Params(10, 100, 1000)]
    public int EntityCount { get; set; }
    
    private TestEntity[] _testEntities = [];
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
    
    /// <summary>
    /// benchmarker: Baseline - InsertBulk WITHOUT Checkpoint (what previous benchmarks measured)
    /// </summary>
    [Benchmark(Baseline = true)]
    public long Write_NoCheckpoint()
    {
        var documents = new List<BsonDocument>(EntityCount);
        
        foreach (var entity in _testEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            documents.Add(new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = json
            });
        }
        
        _collection!.InsertBulk(documents);
        // NO CHECKPOINT - might be buffered in memory!
        
        return new FileInfo(_dbPath).Length;
    }
    
    /// <summary>
    /// benchmarker: InsertBulk WITH Checkpoint (guaranteed disk flush)
    /// If this is significantly slower, previous benchmarks were misleading
    /// </summary>
    [Benchmark]
    public long Write_WithCheckpoint()
    {
        var documents = new List<BsonDocument>(EntityCount);
        
        foreach (var entity in _testEntities)
        {
            var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
            documents.Add(new BsonDocument
            {
                ["_id"] = entity.PersistKey,
                ["data"] = json
            });
        }
        
        _collection!.InsertBulk(documents);
        _db!.Checkpoint(); // EXPLICIT DISK FLUSH
        
        return new FileInfo(_dbPath).Length;
    }
    
    /// <summary>
    /// benchmarker: InsertBulk with Dispose (tests if Dispose flushes)
    /// </summary>
    [Benchmark]
    public long Write_WithDispose()
    {
        // Use a separate DB instance that we can dispose
        var tempPath = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}.db");
        
        try
        {
            using (var tempDb = new LiteDatabase(tempPath))
            {
                var tempCol = tempDb.GetCollection("entities");
                var documents = new List<BsonDocument>(EntityCount);
                
                foreach (var entity in _testEntities)
                {
                    var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
                    documents.Add(new BsonDocument
                    {
                        ["_id"] = entity.PersistKey,
                        ["data"] = json
                    });
                }
                
                tempCol.InsertBulk(documents);
                // Dispose happens here - does it flush?
            }
            
            return new FileInfo(tempPath).Length;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}

/// <summary>
/// benchmarker: Verification test - confirms data is actually on disk
/// </summary>
[MemoryDiagnoser]
public class LiteDbPersistenceVerificationBenchmark
{
    [Params(100)]
    public int EntityCount { get; set; }
    
    private TestEntity[] _testEntities = [];
    private readonly SystemJsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNamingPolicy = SystemJsonNamingPolicy.CamelCase 
    };
    
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
    }
    
    /// <summary>
    /// benchmarker: Write, dispose, re-open, verify - proves data hit disk
    /// </summary>
    [Benchmark]
    public int Write_Dispose_Reopen_Verify()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"verify_{Guid.NewGuid()}.db");
        
        try
        {
            // PHASE 1: Write and dispose
            using (var db = new LiteDatabase(tempPath))
            {
                var col = db.GetCollection("entities");
                var documents = new List<BsonDocument>(EntityCount);
                
                foreach (var entity in _testEntities)
                {
                    var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
                    documents.Add(new BsonDocument
                    {
                        ["_id"] = entity.PersistKey,
                        ["data"] = json
                    });
                }
                
                col.InsertBulk(documents);
            } // Dispose here
            
            // PHASE 2: Re-open and verify all data is present
            using (var db = new LiteDatabase(tempPath))
            {
                var col = db.GetCollection("entities");
                var count = col.Count();
                
                if (count != EntityCount)
                {
                    throw new Exception($"FLUSH FAILED! Expected {EntityCount}, got {count}");
                }
                
                // Verify we can read all entities
                var documents = col.FindAll();
                var readCount = 0;
                foreach (var doc in documents)
                {
                    var json = doc["data"].AsString;
                    var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
                    if (entity != null) readCount++;
                }
                
                return readCount;
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
    
    /// <summary>
    /// benchmarker: Same test but WITH explicit Checkpoint before dispose
    /// </summary>
    [Benchmark]
    public int Write_Checkpoint_Dispose_Reopen_Verify()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"verify_{Guid.NewGuid()}.db");
        
        try
        {
            // PHASE 1: Write, checkpoint, and dispose
            using (var db = new LiteDatabase(tempPath))
            {
                var col = db.GetCollection("entities");
                var documents = new List<BsonDocument>(EntityCount);
                
                foreach (var entity in _testEntities)
                {
                    var json = SystemJsonSerializer.Serialize(entity, _jsonOptions);
                    documents.Add(new BsonDocument
                    {
                        ["_id"] = entity.PersistKey,
                        ["data"] = json
                    });
                }
                
                col.InsertBulk(documents);
                db.Checkpoint(); // EXPLICIT FLUSH
            } // Dispose here
            
            // PHASE 2: Re-open and verify
            using (var db = new LiteDatabase(tempPath))
            {
                var col = db.GetCollection("entities");
                var count = col.Count();
                
                if (count != EntityCount)
                {
                    throw new Exception($"FLUSH FAILED! Expected {EntityCount}, got {count}");
                }
                
                var documents = col.FindAll();
                var readCount = 0;
                foreach (var doc in documents)
                {
                    var json = doc["data"].AsString;
                    var entity = SystemJsonSerializer.Deserialize<TestEntity>(json, _jsonOptions);
                    if (entity != null) readCount++;
                }
                
                return readCount;
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
