using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.Transform;

[MemoryDiagnoser]
public class ScatterApproachBenchmark
{
    private const int TotalSize = 8192;
    private const int ScatterCount = 2048; // ~25% of entities are parents

    // Source data (parent world transforms)
    private float[] _sourceValues = null!;
    private ushort[] _parentIndices = null!; // Which parents are scattered

    // Destination data (child parent transforms)
    private float[] _destValues = null!;
    private ushort[] _childIndices = null!; // Where each parent scatters to

    // Chunked approach (current)
    private float[][] _chunkedSource = null!;
    private float[][] _chunkedDest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        // Single array approach
        _sourceValues = new float[TotalSize];
        _destValues = new float[TotalSize];
        _parentIndices = new ushort[ScatterCount];
        _childIndices = new ushort[ScatterCount];

        for (int i = 0; i < TotalSize; i++)
        {
            _sourceValues[i] = (float)rng.NextDouble();
        }

        // Generate scatter pattern (parent → child mapping)
        var usedIndices = new HashSet<ushort>();
        for (int i = 0; i < ScatterCount; i++)
        {
            // Parent index
            ushort parentIdx;
            do
            {
                parentIdx = (ushort)rng.Next(TotalSize);
            }
            while (usedIndices.Contains(parentIdx));
            
            _parentIndices[i] = parentIdx;
            usedIndices.Add(parentIdx);

            // Child index (different from parent)
            ushort childIdx;
            do
            {
                childIdx = (ushort)rng.Next(TotalSize);
            }
            while (usedIndices.Contains(childIdx));
            
            _childIndices[i] = childIdx;
            usedIndices.Add(childIdx);
        }

        // Chunked arrays (16 elements per chunk, like current approach)
        int chunkSize = 16;
        int chunkCount = TotalSize / chunkSize;
        _chunkedSource = new float[chunkCount][];
        _chunkedDest = new float[chunkCount][];

        for (int chunk = 0; chunk < chunkCount; chunk++)
        {
            _chunkedSource[chunk] = new float[chunkSize];
            _chunkedDest[chunk] = new float[chunkSize];

            for (int i = 0; i < chunkSize; i++)
            {
                _chunkedSource[chunk][i] = _sourceValues[chunk * chunkSize + i];
            }
        }
    }

    [Benchmark(Baseline = true)]
    public void CurrentApproach_ChunkedArrays()
    {
        // Current approach:  iterate parents, lookup chunks, write per element
        for (int i = 0; i < ScatterCount; i++)
        {
            ushort parentIdx = _parentIndices[i];
            ushort childIdx = _childIndices[i];

            int parentChunk = parentIdx / 16;
            int parentLane = parentIdx % 16;
            int childChunk = childIdx / 16;
            int childLane = childIdx % 16;

            float value = _chunkedSource[parentChunk][parentLane];
            _chunkedDest[childChunk][childLane] = value;
        }
    }

    [Benchmark]
    public void SingleArray_DirectIndexing()
    {
        // Single array: direct indexed scatter
        for (int i = 0; i < ScatterCount; i++)
        {
            ushort parentIdx = _parentIndices[i];
            ushort childIdx = _childIndices[i];

            _destValues[childIdx] = _sourceValues[parentIdx];
        }
    }

    [Benchmark]
    public void SingleArray_GatherThenScatter()
    {
        // Gather all parent values into contiguous buffer
        Span<float> gatheredValues = stackalloc float[ScatterCount];
        for (int i = 0; i < ScatterCount; i++)
        {
            gatheredValues[i] = _sourceValues[_parentIndices[i]];
        }

        // Scatter from contiguous buffer to destinations
        for (int i = 0; i < ScatterCount; i++)
        {
            _destValues[_childIndices[i]] = gatheredValues[i];
        }
    }

    [Benchmark]
    public void SingleArray_SpanCopy()
    {
        // For each parent-child pair, use span slicing
        for (int i = 0; i < ScatterCount; i++)
        {
            ushort parentIdx = _parentIndices[i];
            ushort childIdx = _childIndices[i];

            _sourceValues. AsSpan(parentIdx, 1).CopyTo(_destValues.AsSpan(childIdx, 1));
        }
    }
}
