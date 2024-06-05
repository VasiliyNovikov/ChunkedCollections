using System.Numerics;
using System.Runtime.CompilerServices;

namespace ChunkedCollections;

public readonly struct ChunkedSpan<T, TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    private readonly TIndex _offset;
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
            return ref _bucket.At(index + _offset);
        }
    }

    internal ChunkedSpan(TIndex offset, TIndex length, in UnsafeChunkedBucket<T, TIndex> bucket)
    {
        _offset = offset;
        _length = length;
        _bucket = bucket;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> Slice(TIndex start)
    {
        UnsafeChunkedBucket<T, TIndex>.CheckIndexInRange(start, _length);
        return new ChunkedSpan<T, TIndex>(_offset + start, _length - start, _bucket);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> Slice(TIndex start, TIndex length)
    {
        UnsafeChunkedBucket<T, TIndex>.CheckIndexInRange(start, _length);
        UnsafeChunkedBucket<T, TIndex>.CheckIndexInRange(start + length, _length + TIndex.One);
        return new ChunkedSpan<T, TIndex>(_offset + start, length, _bucket);
    }
}