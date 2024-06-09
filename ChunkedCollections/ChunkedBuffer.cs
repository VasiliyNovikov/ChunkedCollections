using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ChunkedCollections;


public readonly struct ChunkedBuffer<T, TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    private readonly T[]?[] _chunks;
    private readonly TIndex _length;
    private readonly int _chunkSize;
    private readonly int _indexInChunkMask;
    private readonly byte _chunkBitSize;

    public TIndex Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length;
    }

    public ChunkedReference<T, TIndex> Start
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ref var reference = ref MemoryMarshal.GetArrayDataReference(_chunks);
            return new ChunkedReference<T, TIndex>(ref reference!, 0, _indexInChunkMask, _chunkBitSize);
        }
    }

    public ref T this[TIndex index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ChunkedCollectionHelper.CheckIndexInRange(index, _length);
            return ref Start.Add(index).Value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ChunkedBuffer(T[]?[] chunks, TIndex length, int chunkSize, int indexInChunkMask, byte chunkBitSize)
    {
        _chunks = chunks;
        _length = length;
        _chunkSize = chunkSize;
        _indexInChunkMask = indexInChunkMask;
        _chunkBitSize = chunkBitSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChunkedBuffer<T, TIndex> Allocate(TIndex length, byte chunkBitSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, TIndex.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkBitSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(chunkBitSize, 30);

        var chunkSize = 1 << chunkBitSize;
        var indexInChunkMask = chunkSize - 1;
        T[]?[] chunks;
        if (TIndex.IsZero(length))
            chunks = [];
        else
        {
            var chunkCount = GetChunkCount(length, chunkBitSize);
            chunks = new T[]?[chunkCount << 1];
            for (var i = 0; i < chunkCount; ++i)
                chunks[i] = new T[chunkSize];
        }
        return new ChunkedBuffer<T, TIndex>(chunks, length, chunkSize, indexInChunkMask, chunkBitSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Resize(ref ChunkedBuffer<T, TIndex> buffer, TIndex newLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newLength, TIndex.Zero);

        var chunks = buffer._chunks;
        var length = buffer.Length;
        var chunkSize = buffer._chunkSize;
        var indexInChunkMask = buffer._indexInChunkMask;
        var chunkBitSize = buffer._chunkBitSize;
        if (newLength == length)
            return;

        var oldChunkCount = GetChunkCount(length, chunkBitSize);
        var newChunkCount = GetChunkCount(newLength, chunkBitSize);

        if (newChunkCount != oldChunkCount)
        {
            if (newChunkCount == 0)
                chunks = [];
            else
            {
                if (oldChunkCount == 0)
                    chunks = new T[]?[newChunkCount << 1];
                else if (newChunkCount <= (oldChunkCount >> 2) || newChunkCount > chunks!.Length)
                    Array.Resize(ref chunks, newChunkCount << 1);

                for (var i = oldChunkCount; i < newChunkCount; ++i)
                    chunks[i] = new T[chunkSize];
            }
        }

        buffer = new(chunks, newLength, chunkSize, indexInChunkMask, chunkBitSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> AsSpan() => new ChunkedSpan<T, TIndex>(Start, _length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> AsSpan(TIndex start)
    {
        ChunkedCollectionHelper.CheckIndexInRange(start, _length);
        return new ChunkedSpan<T, TIndex>(Start.Add(start), _length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> AsSpan(TIndex start, TIndex length)
    {
        ChunkedCollectionHelper.CheckIndexInRange(start, _length);
        ChunkedCollectionHelper.CheckIndexInRange(start + length, _length + TIndex.One);
        return new ChunkedSpan<T, TIndex>(Start.Add(start), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetChunkCount(TIndex count, int chunkBitSize) => Int32.CreateTruncating((count - TIndex.One) >> chunkBitSize) + 1;

    public struct Enumerator(ChunkedBuffer<T, TIndex> buffer)
    {
        private readonly T[]?[] _chunks = buffer._chunks;
        private readonly TIndex _length = buffer._length;
        private readonly int _chunkSize = buffer._chunkSize;
        private readonly int _indexInChunkMask = buffer._indexInChunkMask;
        private readonly byte _chunkBitSize = buffer._chunkBitSize;
        private TIndex _index = TIndex.NegativeOne;
        private int _chunkIndex;
        private T[]? _currentChunk = buffer._length == TIndex.Zero ? null : buffer._chunks[0];
        private int _indexInChunk = -1;

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentChunk![_indexInChunk];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _length)
                return false;

            if (++_indexInChunk == _chunkSize)
            {
                _indexInChunk = 0;
                _currentChunk = _chunks[++_chunkIndex];
            }
            return true;
        }
    }
}