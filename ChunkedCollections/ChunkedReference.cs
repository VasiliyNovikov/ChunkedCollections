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
        var diffWithOffset = TIndex.CreateTruncating(_offset) + diff;
        var chunkIndex = Int32.CreateTruncating(diffWithOffset >> _chunkBitSize);
        var indexInChunk = Int32.CreateTruncating(diffWithOffset) & _indexInChunkMask;
        ref var reference = ref Unsafe.Add(ref _reference, chunkIndex);
        return new ChunkedReference<T, TIndex>(ref reference, indexInChunk, _indexInChunkMask, _chunkBitSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedReference<T, TIndex> Next()
    {
        var newOffset = _offset + 1;
        return (newOffset & _indexInChunkMask) == 0
            ? NextChunk()
            : new(ref _reference, newOffset, _indexInChunkMask, _chunkBitSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedReference<T, TIndex> Prev()
    {
        var offset = _offset;
        return offset == 0
            ? new(ref Unsafe.Add(ref _reference, -1), (1 << _chunkBitSize) - 1, _indexInChunkMask, _chunkBitSize)
            : new(ref _reference, offset - 1, _indexInChunkMask, _chunkBitSize);
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
