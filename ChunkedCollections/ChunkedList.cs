using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ChunkedCollections;

public class ChunkedList<T, TIndex>(int chunkBitSize, TIndex initialCapacity = default) : IEnumerable<T>
     where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    private readonly int _chunkBitSize = chunkBitSize;
    private readonly int _chunkSize = 1 << chunkBitSize;
    private readonly int _indexInChunkMask = (1 << chunkBitSize) - 1;
    private T?[]?[] _chunks = TIndex.IsZero(initialCapacity) ? [] : new T?[]?[GetChunkCount(initialCapacity, chunkBitSize)];
    private TIndex _count;
    private int _nextChunkIndex;
    private int _nextIndexInChunk;

    private static int GetChunkCount(TIndex count, int chunkBitSize)
    {
        if (TIndex.IsZero(count))
            return 0;
        return Int32.CreateTruncating((count - TIndex.One) >> chunkBitSize) + 1;
    }

    public TIndex Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        var chunkIndex = _nextChunkIndex;
        var indexInChunk = _nextIndexInChunk;
        var chunkCount = _chunks.Length;
        if (chunkIndex >= chunkCount)
            Array.Resize(ref _chunks, chunkCount == 0 ? 1 : chunkCount * 2);

        var chunk = _chunks[chunkIndex] ??= new T[_chunkSize];
        chunk[indexInChunk] = item;
        ++_count;
        ++_nextIndexInChunk;
        if (_nextIndexInChunk == _chunkSize)
        {
            _nextIndexInChunk = 0;
            ++_nextChunkIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref T ItemRefAt(TIndex index)
    {
        if (typeof(TIndex) == typeof(long))
        {
            if (UInt64.CreateTruncating(index) >= UInt64.CreateTruncating(_count))
                throw new IndexOutOfRangeException();
        }
        else
        {
            if (UInt32.CreateTruncating(index) >= UInt32.CreateTruncating(_count))
                throw new IndexOutOfRangeException();
        }

        var chunkIndex = Int32.CreateTruncating(index >> _chunkBitSize);
        var indexInChunk = Int32.CreateTruncating(index) & _indexInChunkMask;
        return ref _chunks[chunkIndex]![indexInChunk]!;
    }

    public T this[TIndex index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ItemRefAt(index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => ItemRefAt(index) = value;
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var count = _count;
            var chunkCount = GetChunkCount(count, _chunkBitSize);
            if (chunkCount > 0)
            {
                for (var i = 0; i < chunkCount - 1; ++i)
                    Array.Clear(_chunks[i]!);
                Array.Clear(_chunks[chunkCount - 1]!, 0, (Int32.CreateTruncating(count - TIndex.One) & _indexInChunkMask) + 1);
            }
        }

        _count = TIndex.Zero;
        _nextChunkIndex = 0;
        _nextIndexInChunk = 0;
    }

    public void TrimExcess()
    {
        var chunkCount = GetChunkCount(Count, _chunkBitSize);
        if (chunkCount < _chunks.Length)
            Array.Resize(ref _chunks, chunkCount);
    }

    public void MergeSort() => MergeSort<IComparer<T>>(null);

    public void MergeSort(Comparison<T> comparison) => MergeSort(Comparer<T>.Create(comparison));

    public void MergeSort<TComparer>(TComparer? comparer)
        where TComparer : IComparer<T>?
    {
        var count = _count;
        var chunkBitSize = _chunkBitSize;
        var chunkSize = _chunkSize;
        var indexInChunkMask = _indexInChunkMask;
        if (count <= TIndex.One)
            return;

        var chunkCount = GetChunkCount(count, chunkBitSize);
        var lastChunkSize = (Int32.CreateTruncating(count - TIndex.One) & _indexInChunkMask) + 1;
        if (chunkCount == 1)
        {
            SortChunk(_chunks[0]!.AsSpan(0, lastChunkSize)!, comparer);
            return;
        }

        Memory<T>[] chunks = new Memory<T>[chunkCount];
        for (var i = 0; i < chunkCount - 1; ++i)
        {
            chunks[i] = _chunks[i]!.AsMemory()!;
            _chunks[i] = new T[chunkSize];
        }
        chunks[^1] = _chunks[chunkCount - 1]!.AsMemory(0, lastChunkSize)!;
        _chunks[chunkCount - 1] = new T[chunkSize];
        _count = TIndex.Zero;
        _nextChunkIndex = 0;
        _nextIndexInChunk = 0;

        foreach (var chunk in chunks)
            SortChunk(chunk.Span, comparer);

        // k-way merge
        var sortedItems = new PriorityQueue<TIndex, T>(chunkCount, comparer);
        for (var i = 0; i < chunkCount; ++i)
            sortedItems.Enqueue(TIndex.CreateTruncating(i) << chunkBitSize, chunks[i].Span[0]);

        while (sortedItems.TryDequeue(out var index, out var item))
        {
            Add(item);

            var chunkIndex = Int32.CreateTruncating(index >> chunkBitSize);
            var nextIndexInChunk = (Int32.CreateTruncating(index) & indexInChunkMask) + 1;
            var chunk = chunks[chunkIndex].Span;
            if (nextIndexInChunk < chunk.Length)
                sortedItems.Enqueue(index + TIndex.One, chunk[nextIndexInChunk]);
        }

        static void SortChunk(Span<T> chunk, TComparer? comparer)
        {
            if (comparer is null)
                chunk.Sort();
            else
                chunk.Sort(comparer);
        }
    }

    private static Action<ChunkedList<T, TIndex>>? _defaultQuickSort;

    public void QuickSort()
    {
        var quickSort = _defaultQuickSort;
        if (quickSort is null)
        {
            bool supportsOperations;
            try
            {
                GC.KeepAlive(typeof(IComparisonOperators<,,>).MakeGenericType(typeof(T), typeof(T), typeof(bool)));
                supportsOperations = true;
            }
            catch (ArgumentException)
            {
                supportsOperations = false;
            }

            quickSort = supportsOperations
                ? typeof(ChunkedList<T, TIndex>)
                      .GetMethod(nameof(QuickSortOperations), BindingFlags.Static | BindingFlags.NonPublic)!
                      .MakeGenericMethod(typeof(T))
                      .CreateDelegate<Action<ChunkedList<T, TIndex>>>()
                : throw new NotImplementedException();
            _defaultQuickSort = quickSort;
        }
        quickSort(this);
    }

    public void QuickSort(Comparison<T> comparison) => QuickSort(Comparer<T>.Create(comparison));

    public void QuickSort<TComparer>(TComparer comparer) where TComparer : IComparer<T> => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void QuickSortOperations<U>(ChunkedList<U, TIndex> list) where U : IComparisonOperators<U, U, bool>
    {
        var count = list._count;
        if (count <= TIndex.One)
            return;

        var chunkBitSize = list._chunkBitSize;
        var indexInChunkMask = list._indexInChunkMask;
        Span<U[]> chunks = list._chunks.AsSpan(0, GetChunkCount(count, chunkBitSize))!;

        Sort(chunks, TIndex.Zero, count - TIndex.One, chunkBitSize, indexInChunkMask);

        return;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Sort(Span<U[]> chunks, TIndex start, TIndex end, int chunkBitSize, int indexInChunkMask)
        {
            if (start >= end)
                return;

            var indexDiff = end - start;
            if (indexDiff == TIndex.One)
            {
                ref var startItem = ref At(chunks, start, chunkBitSize, indexInChunkMask);
                ref var endItem = ref At(chunks, end, chunkBitSize, indexInChunkMask);
                if (startItem > endItem)
                    (startItem, endItem) = (endItem, startItem);
                return;
            }

            var startChunkIndex = Int32.CreateTruncating(start >> chunkBitSize);
            var endChunkIndex = Int32.CreateTruncating(end >> chunkBitSize);
            if (startChunkIndex == endChunkIndex)
            {
                var startInChunk = Int32.CreateTruncating(start) & indexInChunkMask;
                chunks[startChunkIndex].AsSpan(startInChunk, Int32.CreateTruncating(indexDiff) + 1).Sort();
            }
            else
            {
                var pivotIndex = Partition(chunks, start, end, chunkBitSize, indexInChunkMask);
                Sort(chunks, start, pivotIndex - TIndex.One, chunkBitSize, indexInChunkMask);
                Sort(chunks, pivotIndex + TIndex.One, end, chunkBitSize, indexInChunkMask);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TIndex Partition(Span<U[]> chunks, TIndex start, TIndex end, int chunkBitSize, int indexInChunkMask)
        {
            ref var pivot = ref At(chunks, end, chunkBitSize, indexInChunkMask);
            ref var median = ref MedianOfThree(chunks, start, end, chunkBitSize, indexInChunkMask);

            Swap(ref median, ref pivot);

            var i = start - TIndex.One;
            for (var j = start; j < end; ++j)
            {
                ref var jItem = ref At(chunks, j, chunkBitSize, indexInChunkMask);
                if (IsLessThan(ref jItem, ref pivot))
                {
                    ++i;
                    Swap(ref At(chunks, i, chunkBitSize, indexInChunkMask), ref jItem);
                }
            }

            var pivotIndex = i + TIndex.One;
            ref var finalPivotRef = ref At(chunks, pivotIndex, chunkBitSize, indexInChunkMask);
            Swap(ref finalPivotRef, ref pivot);
            return pivotIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref U MedianOfThree(Span<U[]> chunks, TIndex start, TIndex end, int chunkBitSize, int indexInChunkMask)
        {
            var middle = (start + end) >> 1;
            ref var startRef = ref At(chunks, start, chunkBitSize, indexInChunkMask);
            ref var middleRef = ref At(chunks, middle, chunkBitSize, indexInChunkMask);
            ref var endRef = ref At(chunks, end, chunkBitSize, indexInChunkMask);

            SwapIfLessThan(ref middleRef, ref startRef);
            SwapIfLessThan(ref endRef, ref startRef);
            SwapIfLessThan(ref endRef, ref middleRef);
            return ref middleRef!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Swap(ref U v1, ref U v2) => (v1, v2) = (v2, v1);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SwapIfLessThan(ref U v1, ref U v2)
        {
            if (IsLessThan(ref v1, ref v2))
                Swap(ref v1, ref v2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref U At(Span<U[]> chunks, TIndex index, int chunkBitSize, int indexInChunkMask)
        {
            var chunkIndex = Int32.CreateTruncating(index >> chunkBitSize);
            var indexInChunk = Int32.CreateTruncating(index) & indexInChunkMask;
            return ref chunks[chunkIndex][indexInChunk];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsLessThan(ref U v1, ref U v2) => v1 < v2;
    }

    public struct Enumerator(ChunkedList<T, TIndex> list) : IEnumerator<T>
    {
        private readonly int _chunkSize = list._chunkSize;
        private readonly T?[]?[] _chunks = list._chunks;
        private readonly TIndex _count = list._count;
        private TIndex _index = TIndex.NegativeOne;
        private int _chunkIndex;
        private T?[]? _currentChunk = list._count == TIndex.Zero ? null : list._chunks[0];
        private int _indexInChunk = -1;

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentChunk![_indexInChunk]!;
        }

        readonly object? IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ++_index;
            if (_index >= _count)
                return false;

            ++_indexInChunk;
            if (_indexInChunk == _chunkSize)
            {
                _indexInChunk = 0;
                ++_chunkIndex;
                _currentChunk = _chunks[_chunkIndex];
            }
            return true;
        }

        public readonly void Dispose()
        {
        }

        public void Reset()
        {
            _index = TIndex.NegativeOne;
            _chunkIndex = 0;
            _currentChunk = list._count == TIndex.Zero ? null : list._chunks[0];
            _indexInChunk = -1;
        }
    }
}

public class ChunkedList<T>(int chunkBitSize, int initialCapacity = 0) : ChunkedList<T, int>(chunkBitSize, initialCapacity);

public class BigChunkedList<T>(int chunkBitSize, long initialCapacity = 0) : ChunkedList<T, long>(chunkBitSize, initialCapacity);