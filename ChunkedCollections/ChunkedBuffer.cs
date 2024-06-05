using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ChunkedCollections;


public readonly struct ChunkedBuffer<T, TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    private readonly TIndex _length;
    private readonly UnsafeChunkedBucket<T, TIndex> _bucket;

    public TIndex Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length;
    }

    public ref T this[TIndex index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            UnsafeChunkedBucket<T, TIndex>.CheckIndexInRange(index, _length);
            return ref _bucket.At(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ChunkedBuffer(TIndex length, in UnsafeChunkedBucket<T, TIndex> bucket)
    {
        _length = length;
        _bucket = bucket;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChunkedBuffer<T, TIndex> Create(TIndex length, byte chunkBitSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, TIndex.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkBitSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(chunkBitSize, 30);

        var chunkSize = 1 << chunkBitSize;
        var indexInChunkMask = chunkSize - 1;
        T[]?[]? chunks;
        if (TIndex.IsZero(length))
            chunks = null;
        else
        {
            var chunkCount = GetChunkCount(length, chunkBitSize);
            chunks = new T[]?[chunkCount << 1];
            for (var i = 0; i < chunkCount; ++i)
                chunks[i] = new T[chunkSize];
        }
        
        var bucket = new UnsafeChunkedBucket<T, TIndex>
        {
            Chunks = chunks,
            ChunkSize = chunkSize,
            IndexInChunkMask = indexInChunkMask,
            ChunkBitSize = chunkBitSize
        };
        return new ChunkedBuffer<T, TIndex>(length, bucket);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Resize(ref ChunkedBuffer<T, TIndex> buffer, TIndex newLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newLength, TIndex.Zero);

        var length = buffer.Length;
        if (newLength == length)
            return;

        var bucket = buffer._bucket;
        var oldChunkCount = GetChunkCount(length, bucket.ChunkBitSize);
        var newChunkCount = GetChunkCount(newLength, bucket.ChunkBitSize);

        if (newChunkCount != oldChunkCount)
        {
            if (newChunkCount == 0)
                bucket.Chunks = null;
            else
            {
                if (oldChunkCount == 0)
                    bucket.Chunks = new T[]?[newChunkCount << 1];
                else if (newChunkCount <= (oldChunkCount >> 2) || newChunkCount > bucket.Chunks!.Length)
                    Array.Resize(ref bucket.Chunks, newChunkCount << 1);

                for (var i = oldChunkCount; i < newChunkCount; ++i)
                    bucket.Chunks[i] = new T[bucket.ChunkSize];
            }
        }

        buffer = new(newLength, bucket);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> AsSpan() => new ChunkedSpan<T, TIndex>(TIndex.Zero, _length, _bucket);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> AsSpan(TIndex offset)
    {
        UnsafeChunkedBucket<T, TIndex>.CheckIndexInRange(offset, _length);
        return new ChunkedSpan<T, TIndex>(offset, _length - offset, _bucket);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> AsSpan(TIndex offset, TIndex length)
    {
        UnsafeChunkedBucket<T, TIndex>.CheckIndexInRange(offset, _length);
        UnsafeChunkedBucket<T, TIndex>.CheckIndexInRange(offset + length, _length + TIndex.One);
        return new ChunkedSpan<T, TIndex>(offset, length, _bucket);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetChunkCount(TIndex count, int chunkBitSize) => Int32.CreateTruncating((count - TIndex.One) >> chunkBitSize) + 1;
}