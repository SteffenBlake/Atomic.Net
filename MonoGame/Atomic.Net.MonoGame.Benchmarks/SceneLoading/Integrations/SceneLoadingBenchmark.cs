// benchmarker: Scene Loading Performance - JSON Parsing vs Entity Spawning Breakdown
// This benchmark measures what percentage of total scene load time is spent on:
// 1. JSON parsing (File.ReadAllText + JsonSerializer.Deserialize)
// 2. Entity spawning + behavior application (the full loop)
//
// Purpose: Determine if async scene loading provides meaningful parallelism or if
// most work still blocks the main thread during entity spawning phase.

using System.Diagnostics;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Benchmarks.SceneLoading.Integrations;

/// <summary>
/// Integration benchmark for scene loading performance breakdown.
/// Tests a large scene with 1000 entities containing Transform, Properties, Tags, and Parent behaviors.
/// Measures: (1) JSON parsing time, (2) Full entity spawning + behavior application time.
/// </summary>
[MemoryDiagnoser]
public class SceneLoadingBenchmark
{
    private const string ScenePath = "SceneLoading/Fixtures/large-scene.json";
    private string _jsonText = "";
    private JsonScene _scene = null!;
    
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerOptions.Web)
    {
        RespectRequiredConstructorParameters = true
    };
    
    [GlobalSetup]
    public void Setup()
    {
        // Initialize the Atomic system FIRST (required for JSON deserialization)
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        // Ensure scene file exists and is loaded
        if (!File.Exists(ScenePath))
        {
            throw new FileNotFoundException($"Scene file not found: {ScenePath}");
        }
        
        // Pre-load JSON text for parsing benchmarks
        _jsonText = File.ReadAllText(ScenePath);
        
        // Pre-parse scene object for entity spawning benchmarks
        _scene = JsonSerializer.Deserialize<JsonScene>(_jsonText, _serializerOptions) ?? new();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Clean up entities between benchmark runs
        EventBus<ShutdownEvent>.Push(new());
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Reset entities between iterations
        EventBus<ResetEvent>.Push(new());
    }

    /// <summary>
    /// Baseline: Measure ONLY the JSON parsing time (File.ReadAllText + Deserialize to JsonScene).
    /// This represents the work that could potentially be done on a background thread.
    /// </summary>
    [Benchmark(Baseline = true)]
    public JsonScene? JsonParsing_Only()
    {
        // Read file (in real scenarios, this might be cached)
        var jsonText = File.ReadAllText(ScenePath);
        
        // Parse JSON into JsonScene object
        var scene = JsonSerializer.Deserialize<JsonScene>(jsonText, _serializerOptions);
        
        return scene;
    }

    /// <summary>
    /// Measure ONLY the entity spawning + behavior application loop.
    /// This represents the work that MUST happen on the main thread.
    /// Assumes JSON has already been parsed (not measured here).
    /// </summary>
    [Benchmark]
    public int EntitySpawning_Only()
    {
        // Use pre-parsed scene object from Setup() - deserialization is NOT measured here
        var entityCount = 0;
        
        foreach (var jsonEntity in _scene.Entities)
        {
            var entity = EntityRegistry.Instance.Activate();
            jsonEntity.WriteToEntity(entity);
            entityCount++;
        }
        
        // Recalc selectors and hierarchy (part of scene loading)
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();
        
        return entityCount;
    }

    /// <summary>
    /// Measure the FULL scene loading pipeline (JSON parsing + entity spawning).
    /// This is the total time for SceneLoader.LoadGameScene().
    /// </summary>
    [Benchmark]
    public int FullSceneLoad()
    {
        // This simulates the full LoadSceneInternal workflow
        
        // Step 1: Parse JSON
        var jsonText = File.ReadAllText(ScenePath);
        var scene = JsonSerializer.Deserialize<JsonScene>(jsonText, _serializerOptions);
        
        if (scene == null)
        {
            return 0;
        }
        
        // Step 2: Spawn entities and apply behaviors
        var entityCount = 0;
        
        foreach (var jsonEntity in scene.Entities)
        {
            var entity = EntityRegistry.Instance.Activate();
            jsonEntity.WriteToEntity(entity);
            entityCount++;
        }
        
        // Step 3: Recalc selectors and hierarchy
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();
        
        return entityCount;
    }
}
