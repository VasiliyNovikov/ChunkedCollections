using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ChunkedCollections;

public readonly ref struct ChunkedReference<T, TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    private readonly ref T[] _reference;
    private readonly int _offset;
    private readonly int _indexInChunkMask;
    private readonly byte _chunkBitSize;

    public ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _reference[_offset];
    }

    internal int Offset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _offset;
    }

    internal byte ChunkBitSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chunkBitSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ChunkedReference(ref T[] reference, int offset, int indexInChunkMask, byte chunkBitSize)
    {
        _reference = ref reference;
        _offset = offset;
        _indexInChunkMask = indexInChunkMask;
        _chunkBitSize = chunkBitSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedReference<T, TIndex> Add(TIndex diff)
    {
        var chunkBitSize = _chunkBitSize;
        var indexInChunkMask = _indexInChunkMask;
        var diffWithOffset = TIndex.CreateTruncating(_offset) + diff;
        var chunkIndex = Int32.CreateTruncating(diffWithOffset >> chunkBitSize);
        var indexInChunk = Int32.CreateTruncating(diffWithOffset) & indexInChunkMask;
        ref var reference = ref Unsafe.Add(ref _reference, chunkIndex);
        return new ChunkedReference<T, TIndex>(ref reference, indexInChunk, indexInChunkMask, chunkBitSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedReference<T, TIndex> Next()
    {
        var indexInChunkMask = _indexInChunkMask;
        var newOffset = _offset + 1;
        return (newOffset & indexInChunkMask) == 0
            ? new(ref Unsafe.Add(ref _reference, 1), 0, indexInChunkMask, _chunkBitSize)
            : new(ref _reference, newOffset, indexInChunkMask, _chunkBitSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedReference<T, TIndex> Prev()
    {
        var chunkBitSize = _chunkBitSize;
        var indexInChunkMask = _indexInChunkMask;
        var offset = _offset;
        return offset == 0
            ? new(ref Unsafe.Add(ref _reference, -1), (1 << chunkBitSize) - 1, indexInChunkMask, chunkBitSize)
            : new(ref _reference, offset - 1, indexInChunkMask, chunkBitSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedReference<T, TIndex> NextChunk() => new(ref Unsafe.Add(ref _reference, 1), 0, _indexInChunkMask, _chunkBitSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedReference<T, TIndex> PrevChunk() => new(ref Unsafe.Add(ref _reference, -1), 0, _indexInChunkMask, _chunkBitSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChunkedReference<T, TIndex> operator +(ChunkedReference<T, TIndex> left, TIndex right) => left.Add(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChunkedReference<T, TIndex> operator ++(ChunkedReference<T, TIndex> value) => value.Next();
}
