using System.Numerics;
using System.Runtime.CompilerServices;

namespace ChunkedCollections;

public readonly ref struct ChunkedSpan<T, TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    private readonly ChunkedReference<T, TIndex> _reference;
    private readonly TIndex _length;

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
            ChunkedCollectionHelper.CheckIndexInRange(index, _length);
            return ref _reference.Add(index).Value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ChunkedSpan(ChunkedReference<T, TIndex> reference, TIndex length)
    {
        _reference = reference;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> Slice(TIndex start)
    {
        ChunkedCollectionHelper.CheckIndexInRange(start, _length);
        return new ChunkedSpan<T, TIndex>(_reference.Add(start), _length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkedSpan<T, TIndex> Slice(TIndex start, TIndex length)
    {
        ChunkedCollectionHelper.CheckIndexInRange(start, _length);
        ChunkedCollectionHelper.CheckIndexInRange(start + length, _length + TIndex.One);
        return new ChunkedSpan<T, TIndex>(_reference.Add(start), length);
    }
}