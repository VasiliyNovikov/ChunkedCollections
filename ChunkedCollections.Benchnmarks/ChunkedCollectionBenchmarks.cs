using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;
using System.Collections.Generic;

namespace ChunkedCollections.Benchmarks;

using ChunkedBuffer32 = ChunkedBuffer<long, int>;
using ChunkedBuffer64 = ChunkedBuffer<long, long>;


[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class ChunkedCollectionBenchmarks
{
    private const int Size = 1_000_000;
    private const int ChunkBitSize = 12;

    private ChunkedList32 _chunkedList32 = null!;
    private ChunkedList64 _chunkedList64 = null!;
    private ChunkedBuffer32 _chunkedBuffer32 = default;
    private ChunkedBuffer64 _chunkedBuffer64 = default;
    private List<long> _list = null!;
    private Queue<long> _queue = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _chunkedList32 = Populate_Capacity_ChunkedList_32();
        _chunkedList64 = Populate_Capacity_ChunkedList_64();
        _chunkedBuffer32 = Populate_Capacity_ChunkedBuffer_32();
        _chunkedBuffer64 = Populate_Capacity_ChunkedBuffer_64();
        _list = Populate_Capacity_List();
        _queue = Populate_Capacity_Queue();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Populate")]
    public List<long> Populate_List()
    {
        var list = new List<long>();
        for (long i = 0; i < Size; ++i)
            list.Add(i);
        return list;
    }

    [Benchmark]
    [BenchmarkCategory("Populate")]
    public ChunkedList32 Populate_ChunkedList_32()
    {
        var list = new ChunkedList32();
        for (long i = 0; i < Size; ++i)
            list.Add(i);
        return list;
    }

    [Benchmark]
    [BenchmarkCategory("Populate")]
    public ChunkedList64 Populate_ChunkedList_64()
    {
        var list = new ChunkedList64();
        for (long i = 0; i < Size; ++i)
            list.Add(i);
        return list;
    }

    [Benchmark]
    [BenchmarkCategory("Populate")]
    public Queue<long> Populate_Queue() {
        var queue = new Queue<long>();
        for (long i = 0; i < Size; ++i)
            queue.Enqueue(i);
        return queue;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Populate Capacity")]
    public List<long> Populate_Capacity_List()
    {
        var list = new List<long>(Size);
        for (long i = 0; i < Size; ++i)
            list.Add(i);
        return list;
    }

    [Benchmark]
    [BenchmarkCategory("Populate Capacity")]
    public ChunkedList32 Populate_Capacity_ChunkedList_32()
    {
        var list = new ChunkedList32(Size);
        for (long i = 0; i < Size; ++i)
            list.Add(i);
        return list;
    }

    [Benchmark]
    [BenchmarkCategory("Populate Capacity")]
    public ChunkedList64 Populate_Capacity_ChunkedList_64()
    {
        var list = new ChunkedList64(Size);
        for (long i = 0; i < Size; ++i)
            list.Add(i);
        return list;
    }

    [Benchmark]
    [BenchmarkCategory("Populate Capacity")]
    public ChunkedBuffer32 Populate_Capacity_ChunkedBuffer_32()
    {
        var buffer = ChunkedBuffer32.Allocate(Size, ChunkBitSize);
        for (int i = 0; i < Size; ++i)
            buffer[i] = i;
        return buffer;
    }

    [Benchmark]
    [BenchmarkCategory("Populate Capacity")]
    public ChunkedBuffer64 Populate_Capacity_ChunkedBuffer_64()
    {
        var buffer = ChunkedBuffer64.Allocate(Size, ChunkBitSize);
        for (long i = 0; i < Size; ++i)
            buffer[i] = i;
        return buffer;
    }

    [Benchmark]
    [BenchmarkCategory("Populate Capacity")]
    public ChunkedBuffer32 Populate_Capacity_ChunkedSpan_32()
    {
        var buffer = ChunkedBuffer32.Allocate(Size, ChunkBitSize);
        var bufferSpan = buffer.AsSpan();
        for (int i = 0; i < Size; ++i)
            bufferSpan[i] = i;
        return buffer;
    }

    [Benchmark]
    [BenchmarkCategory("Populate Capacity")]
    public ChunkedBuffer64 Populate_Capacity_ChunkedSpan_64()
    {
        var buffer = ChunkedBuffer64.Allocate(Size, ChunkBitSize);
        var bufferSpan = buffer.AsSpan();
        for (long i = 0; i < Size; ++i)
            bufferSpan[i] = i;
        return buffer;
    }

    [Benchmark]
    [BenchmarkCategory("Populate Capacity")]
    public Queue<long> Populate_Capacity_Queue()
    {
        var queue = new Queue<long>((int)Size);
        for (long i = 0; i < Size; ++i)
            queue.Enqueue(i);
        return queue;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Enumerate")]
    public long Enumerate_List()
    {
        long sum = 0;
        foreach (var item in _list)
            sum += item;
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Enumerate")]
    public long Enumerate_ChunkedList_32()
    {
        long sum = 0;
        foreach (var item in _chunkedList32)
            sum += item;
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Enumerate")]
    public long Enumerate_ChunkedList_64()
    {
        long sum = 0;
        foreach (var item in _chunkedList64)
            sum += item;
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Enumerate")]
    public long Enumerate_ChunkedBuffer_32()
    {
        long sum = 0;
        foreach (var item in _chunkedBuffer32)
            sum += item;
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Enumerate")]
    public long Enumerate_ChunkedBuffer_64()
    {
        long sum = 0;
        foreach (var item in _chunkedBuffer64)
            sum += item;
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Enumerate")]
    public long Enumerate_ChunkedSpan_32()
    {
        long sum = 0;
        foreach (var item in _chunkedBuffer32.AsSpan())
            sum += item;
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Enumerate")]
    public long Enumerate_ChunkedSpan_64()
    {
        long sum = 0;
        foreach (var item in _chunkedBuffer64.AsSpan())
            sum += item;
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Enumerate")]
    public long Enumerate_Queue()
    {
        long sum = 0;
        foreach (var item in _queue)
            sum += item;
        return sum;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Index")]
    public long Index_List()
    {
        long sum = 0;
        var list = _list;
        for (int i = 0; i < list.Count; ++i)
            sum += list[i];
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Index")]
    public long Index_ChunkedList_32()
    {
        long sum = 0;
        var list = _chunkedList32;
        for (int i = 0; i < list.Count; ++i)
            sum += list[i];
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Index")]
    public long Index_ChunkedList_64()
    {
        long sum = 0;
        var list = _chunkedList64;
        for (long i = 0; i < list.Count; ++i)
            sum += list[i];
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Index")]
    public long Index_ChunkedBuffer_32()
    {
        long sum = 0;
        var buffer = _chunkedBuffer32;
        for (int i = 0; i < buffer.Length; ++i)
            sum += buffer[i];
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Index")]
    public long Index_ChunkedBuffer_64()
    {
        long sum = 0;
        var buffer = _chunkedBuffer64;
        for (long i = 0; i < buffer.Length; ++i)
            sum += buffer[i];
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Index")]
    public long Index_ChunkedSpan_32()
    {
        long sum = 0;
        var span = _chunkedBuffer32.AsSpan();
        for (int i = 0; i < span.Length; ++i)
            sum += span[i];
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Index")]
    public long Index_ChunkedSpan_64()
    {
        long sum = 0;
        var span = _chunkedBuffer64.AsSpan();
        for (long i = 0; i < span.Length; ++i)
            sum += span[i];
        return sum;
    }


    private List<long> _sortList = null!;

    [IterationSetup(Target = nameof(Sort_List))]
    public void Sort_List_Setup()
    {
        _sortList = new List<long>(Size);
        var random = new Random(42);
        for (long i = 0; i < Size; ++i)
            _sortList.Add(random.NextInt64(Size));
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Sort")]
    public long Sort_List()
    {
        _list.Sort();
        return _list[0];
    }

    private ChunkedList32 _sortChunkedList32 = null!;

    [IterationSetup(Targets = [nameof(MergeSort_ChunkedList_32), nameof(QuickSort_ChunkedList_32)])]
    public void Sort_ChunkedList_32_Setup()
    {
        _sortChunkedList32 = new ChunkedList32(Size);
        var random = new Random(42);
        for (long i = 0; i < Size; ++i)
            _sortChunkedList32.Add(random.NextInt64(Size));
    }

    //[Benchmark]
    [BenchmarkCategory("Sort")]
    public long MergeSort_ChunkedList_32()
    {
        _sortChunkedList32.MergeSort();
        return _sortChunkedList32[0];
    }

    [Benchmark]
    [BenchmarkCategory("Sort")]
    public long QuickSort_ChunkedList_32()
    {
        _sortChunkedList32.QuickSort();
        return _sortChunkedList32[0];
    }

    private ChunkedList64 _sortChunkedList64 = null!;

    [IterationSetup(Targets = [nameof(MergeSort_ChunkedList_64), nameof(QuickSort_ChunkedList_64)])]
    public void Sort_ChunkedList_64_Setup()
    {
        _sortChunkedList64 = new ChunkedList64(Size);
        var random = new Random(42);
        for (long i = 0; i < Size; ++i)
            _sortChunkedList64.Add(random.NextInt64(Size));
    }

    //[Benchmark]
    [BenchmarkCategory("Sort")]
    public long MergeSort_ChunkedList_64()
    {
        _sortChunkedList64.MergeSort();
        return _sortChunkedList64[0];
    }

    [Benchmark]
    [BenchmarkCategory("Sort")]
    public long QuickSort_ChunkedList_64()
    {
        _sortChunkedList64.QuickSort();
        return _sortChunkedList64[0];
    }

    private ChunkedBuffer32 _sortChunkedBuffer32 = default;

    [IterationSetup(Targets = [nameof(Sort_ChunkedBuffer_32)])]
    public void Sort_ChunkedBuffer_32_Setup()
    {
        _sortChunkedBuffer32 = ChunkedBuffer32.Allocate(Size, ChunkBitSize);
        var random = new Random(42);
        for (int i = 0; i < Size; ++i)
            _sortChunkedBuffer32[i] = random.NextInt64(Size);
    }

    [Benchmark]
    [BenchmarkCategory("Sort")]
    public long Sort_ChunkedBuffer_32()
    {
        _sortChunkedBuffer32.AsSpan().Sort();
        return _sortChunkedBuffer32[0];
    }

    private ChunkedBuffer64 _sortChunkedBuffer64 = default;

    [IterationSetup(Targets = [nameof(Sort_ChunkedBuffer_64)])]
    public void Sort_ChunkedBuffer_64_Setup()
    {
        _sortChunkedBuffer64 = ChunkedBuffer64.Allocate(Size, ChunkBitSize);
        var random = new Random(42);
        for (long i = 0; i < Size; ++i)
            _sortChunkedBuffer64[i] = random.NextInt64(Size);
    }

    [Benchmark]
    [BenchmarkCategory("Sort")]
    public long Sort_ChunkedBuffer_64()
    {
        _sortChunkedBuffer64.AsSpan().Sort();
        return _sortChunkedBuffer64[0];
    }

    public sealed class ChunkedList32(int initialCapacity = 0) : ChunkedList<long>(ChunkBitSize, initialCapacity);
    public sealed class ChunkedList64(long initialCapacity = 0) : BigChunkedList<long>(ChunkBitSize, initialCapacity);
}