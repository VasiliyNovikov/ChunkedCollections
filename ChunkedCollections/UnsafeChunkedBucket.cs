using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ChunkedCollections;

internal struct UnsafeChunkedBucket<T, TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    public T[]?[]? Chunks;
    public int ChunkSize;
    public int IndexInChunkMask;
    public byte ChunkBitSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T At(TIndex index)
    {
        var chunkIndex = Int32.CreateTruncating(index >> ChunkBitSize);
        var indexInChunk = Int32.CreateTruncating(index) & IndexInChunkMask;
        return ref Chunks![chunkIndex]![indexInChunk]!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckIndexInRange(TIndex index, TIndex length)
    {
        if (typeof(TIndex) == typeof(long))
        {
            if ((ulong)(long)(object)index >= (ulong)(long)(object)length)
                throw new IndexOutOfRangeException();
        }
        else
        {
            if ((uint)(int)(object)index >= (uint)(int)(object)length)
                throw new IndexOutOfRangeException();
        }
    }
}
