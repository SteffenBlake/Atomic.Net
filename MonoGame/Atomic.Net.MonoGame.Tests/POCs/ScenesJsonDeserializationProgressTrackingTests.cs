using System.Text.Json;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;
using Xunit;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests.POCs.Scenes;

/// <summary>
/// Spike test to determine if byte-level progress can be tracked during JSON scene deserialization.
/// Tests whether JsonSerializer.DeserializeAsync reads incrementally or buffers up front.
/// NOTE: This file should be in POCs/Scenes/ directory once that directory is created.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class JsonDeserializationProgressTrackingTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ErrorEventLogger _errorLogger;
    private readonly string _largeSceneJsonPath;

    public JsonDeserializationProgressTrackingTests(ITestOutputHelper output)
    {
        _output = output;
        _errorLogger = new ErrorEventLogger(output);

        // Arrange: Use existing large scene file from benchmarks
        _largeSceneJsonPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "Atomic.Net.MonoGame.Benchmarks",
            "SceneLoading",
            "Fixtures",
            "large-scene.json"
        );

        // Arrange: Initialize systems
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        _errorLogger.Dispose();
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void Deserialize_WithProgressTracking_LogsReadCallsIncrementally()
    {
        // Arrange
        var progressEvents = new List<StreamProgressEvent>();
        var progress = new Progress<StreamProgressEvent>(e =>
        {
            progressEvents.Add(e);
            _output.WriteLine($"[{e.Timestamp:HH:mm:ss.fff}] Read {e.BytesRead} bytes (total: {e.TotalBytesRead})");
        });

        Assert.True(File.Exists(_largeSceneJsonPath), $"Large scene file not found: {_largeSceneJsonPath}");

        using var fileStream = File.OpenRead(_largeSceneJsonPath);
        using var trackingStream = new ProgressTrackingStream(fileStream, progress);

        // Act
        var scene = JsonSerializer.Deserialize<JsonScene>(trackingStream, JsonSerializerOptions.Web);

        // Assert
        Assert.NotNull(scene);
        Assert.NotEmpty(scene.Entities);

        // Verify progress was tracked
        Assert.NotEmpty(progressEvents);
        _output.WriteLine($"\nTotal read operations: {progressEvents.Count}");
        _output.WriteLine($"Total bytes read: {progressEvents[^1].TotalBytesRead}");
        _output.WriteLine($"File size: {fileStream.Length}");

        // Verify all bytes were read
        Assert.Equal(fileStream.Length, progressEvents[^1].TotalBytesRead);

        // Log findings for analysis
        if (progressEvents.Count == 1)
        {
            _output.WriteLine("\nFINDING: JsonSerializer buffered entire file in ONE read operation.");
            _output.WriteLine("CONCLUSION: Byte-level progress tracking is NOT viable during deserialization.");
            _output.WriteLine("RECOMMENDATION: Track progress via entities spawned instead of bytes read.");
        }
        else
        {
            _output.WriteLine($"\nFINDING: JsonSerializer read file in {progressEvents.Count} incremental operations.");
            _output.WriteLine("CONCLUSION: Byte-level progress tracking IS viable during deserialization.");
            _output.WriteLine($"Average read size: {progressEvents[^1].TotalBytesRead / progressEvents.Count} bytes");

            var readSizes = progressEvents.Select(e => e.BytesRead).ToList();
            _output.WriteLine($"Min read size: {readSizes.Min()} bytes");
            _output.WriteLine($"Max read size: {readSizes.Max()} bytes");
        }
    }

    [Fact]
    public async Task DeserializeAsync_WithProgressTracking_LogsReadCallsIncrementallyAsync()
    {
        // Arrange
        var progressEvents = new List<StreamProgressEvent>();
        var progress = new Progress<StreamProgressEvent>(e =>
        {
            progressEvents.Add(e);
            _output.WriteLine($"[{e.Timestamp:HH:mm:ss.fff}] ReadAsync {e.BytesRead} bytes (total: {e.TotalBytesRead})");
        });

        Assert.True(File.Exists(_largeSceneJsonPath), $"Large scene file not found: {_largeSceneJsonPath}");

        await using var fileStream = File.OpenRead(_largeSceneJsonPath);
        await using var trackingStream = new ProgressTrackingStream(fileStream, progress);

        // Act
        var scene = await JsonSerializer.DeserializeAsync<JsonScene>(trackingStream, JsonSerializerOptions.Web);

        // Assert
        Assert.NotNull(scene);
        Assert.NotEmpty(scene.Entities);

        // Verify progress was tracked
        Assert.NotEmpty(progressEvents);
        _output.WriteLine($"\nTotal read operations: {progressEvents.Count}");
        _output.WriteLine($"Total bytes read: {progressEvents[^1].TotalBytesRead}");
        _output.WriteLine($"File size: {fileStream.Length}");

        // Verify all bytes were read
        Assert.Equal(fileStream.Length, progressEvents[^1].TotalBytesRead);

        // Log findings for analysis
        if (progressEvents.Count == 1)
        {
            _output.WriteLine("\nFINDING: JsonSerializer.DeserializeAsync buffered entire file in ONE read operation.");
            _output.WriteLine("CONCLUSION: Byte-level progress tracking is NOT viable during async deserialization.");
            _output.WriteLine("RECOMMENDATION: Track progress via entities spawned instead of bytes read.");
        }
        else
        {
            _output.WriteLine($"\nFINDING: JsonSerializer.DeserializeAsync read file in {progressEvents.Count} incremental operations.");
            _output.WriteLine("CONCLUSION: Byte-level progress tracking IS viable during async deserialization.");
            _output.WriteLine($"Average read size: {progressEvents[^1].TotalBytesRead / progressEvents.Count} bytes");

            var readSizes = progressEvents.Select(e => e.BytesRead).ToList();
            _output.WriteLine($"Min read size: {readSizes.Min()} bytes");
            _output.WriteLine($"Max read size: {readSizes.Max()} bytes");
        }
    }

    [Fact]
    public async Task DeserializeAsync_WithLargeFile_TracksProgressOverTime()
    {
        // Arrange: Create a VERY large JSON file (10+ MB) to test progressive reading
        var largeTempFile = Path.Combine(Path.GetTempPath(), $"atomic-net-large-test-{Guid.NewGuid()}.json");

        try
        {
            // Generate a large scene with many entities
            var largeScene = new JsonScene();
            for (var i = 0; i < 10000; i++)
            {
                largeScene.Entities.Add(new JsonEntity
                {
                    Transform = new()
                    {
                        Position = new() { X = i, Y = i, Z = i },
                        Rotation = new() { X = 0, Y = 0, Z = 0, W = 1 },
                        Scale = new() { X = 1, Y = 1, Z = 1 }
                    }
                });
            }

            // Write large scene to temp file
            await using (var writeStream = File.Create(largeTempFile))
            {
                await JsonSerializer.SerializeAsync(writeStream, largeScene, JsonSerializerOptions.Web);
            }

            var fileSize = new FileInfo(largeTempFile).Length;
            _output.WriteLine($"Generated large test file: {fileSize:N0} bytes ({fileSize / 1024.0 / 1024.0:F2} MB)");

            // Arrange: Track progress
            var progressEvents = new List<StreamProgressEvent>();
            var progress = new Progress<StreamProgressEvent>(e =>
            {
                progressEvents.Add(e);
                var percentComplete = (double)e.TotalBytesRead / fileSize * 100.0;
                _output.WriteLine($"[{e.Timestamp:HH:mm:ss.fff}] Read {e.BytesRead} bytes | Total: {e.TotalBytesRead:N0} / {fileSize:N0} ({percentComplete:F1}%)");
            });

            await using var fileStream = File.OpenRead(largeTempFile);
            await using var trackingStream = new ProgressTrackingStream(fileStream, progress);

            // Act
            var scene = await JsonSerializer.DeserializeAsync<JsonScene>(trackingStream, JsonSerializerOptions.Web);

            // Assert
            Assert.NotNull(scene);
            Assert.Equal(10000, scene.Entities.Count);

            // Verify progress was tracked
            Assert.NotEmpty(progressEvents);
            _output.WriteLine($"\n=== PROGRESS TRACKING ANALYSIS ===");
            _output.WriteLine($"Total read operations: {progressEvents.Count}");
            _output.WriteLine($"Total bytes read: {progressEvents[^1].TotalBytesRead:N0}");
            _output.WriteLine($"File size: {fileSize:N0}");

            // Calculate time distribution
            if (progressEvents.Count > 1)
            {
                var startTime = progressEvents[0].Timestamp;
                var endTime = progressEvents[^1].Timestamp;
                var totalDuration = endTime - startTime;

                _output.WriteLine($"\nTiming:");
                _output.WriteLine($"  First read: {startTime:HH:mm:ss.fff}");
                _output.WriteLine($"  Last read: {endTime:HH:mm:ss.fff}");
                _output.WriteLine($"  Total duration: {totalDuration.TotalMilliseconds:F2}ms");

                _output.WriteLine($"\nRead sizes:");
                var readSizes = progressEvents.Select(e => e.BytesRead).ToList();
                _output.WriteLine($"  Min: {readSizes.Min():N0} bytes");
                _output.WriteLine($"  Max: {readSizes.Max():N0} bytes");
                _output.WriteLine($"  Average: {readSizes.Average():F0} bytes");
            }

            // Document findings
            _output.WriteLine($"\n=== FINDINGS ===");
            if (progressEvents.Count == 1)
            {
                _output.WriteLine("JsonSerializer.DeserializeAsync buffered entire file in ONE operation.");
                _output.WriteLine("=> Byte-level progress tracking is NOT viable.");
                _output.WriteLine("=> RECOMMENDATION: Track entities spawned, not bytes read.");
            }
            else
            {
                _output.WriteLine($"JsonSerializer.DeserializeAsync performed {progressEvents.Count} incremental reads.");
                _output.WriteLine("=> Byte-level progress tracking IS viable for loading bars.");
                _output.WriteLine($"=> Can report progress every ~{progressEvents[^1].TotalBytesRead / progressEvents.Count:N0} bytes.");
            }
        }
        finally
        {
            // Cleanup: Delete temp file
            if (File.Exists(largeTempFile))
            {
                File.Delete(largeTempFile);
            }
        }
    }
}
