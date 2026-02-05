using System.Numerics.Tensors;
using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.Transform;

[MemoryDiagnoser]
public class TensorChunkingBenchmark
{
    private const int TotalSize = 8192;

    [Params(16, 32, 64, 128, 256, 512, 1024)]
    public int ChunkSize { get; set; }

    private float[] _wholeArray = null!;
    private float[][] _chunkedArrays = null!;
    private float[] _outputWhole = null!;
    private float[][] _outputChunked = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        // Whole array
        _wholeArray = new float[TotalSize];
        _outputWhole = new float[TotalSize];
        for (int i = 0; i < TotalSize; i++)
        {
            _wholeArray[i] = (float)rng.NextDouble();
        }

        // Chunked arrays
        int chunkCount = TotalSize / ChunkSize;
        _chunkedArrays = new float[chunkCount][];
        _outputChunked = new float[chunkCount][];

        for (int chunk = 0; chunk < chunkCount; chunk++)
        {
            _chunkedArrays[chunk] = new float[ChunkSize];
            _outputChunked[chunk] = new float[ChunkSize];

            for (int i = 0; i < ChunkSize; i++)
            {
                _chunkedArrays[chunk][i] = _wholeArray[chunk * ChunkSize + i];
            }
        }
    }

    [Benchmark(Baseline = true)]
    public void WholeArray_NoChunking()
    {
        // Single SIMD operation over entire array
        TensorPrimitives.Exp(_wholeArray, _outputWhole);
    }

    [Benchmark]
    public void ChunkedArrays_ArrayOfArrays()
    {
        // Multiple SIMD operations over array-of-arrays
        for (int chunk = 0; chunk < _chunkedArrays.Length; chunk++)
        {
            TensorPrimitives.Exp(_chunkedArrays[chunk], _outputChunked[chunk]);
        }
    }

    [Benchmark]
    public void ChunkedSpans_SingleArray()
    {
        // Multiple SIMD operations over spans of single array
        int chunkCount = TotalSize / ChunkSize;

        for (int chunk = 0; chunk < chunkCount; chunk++)
        {
            int offset = chunk * ChunkSize;
            var inputSpan = _wholeArray.AsSpan(offset, ChunkSize);
            var outputSpan = _outputWhole.AsSpan(offset, ChunkSize);

            TensorPrimitives.Exp(inputSpan, outputSpan);
        }
    }
}
