using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ChunkedCollections;

[DebuggerTypeProxy(typeof(ChunkedSpanDebugView<,>))]
public readonly ref struct ChunkedSpan<T, TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    private readonly ChunkedReference<T, TIndex> _reference;
    private readonly TIndex _length;

    public ChunkedReference<T, TIndex> Start
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _reference;
    }

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

    public bool IsSingleChunk
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var reference = _reference;
            return _length + TIndex.CreateTruncating(reference.Offset) <= TIndex.CreateTruncating(1 << reference.ChunkBitSize);
        }
    }

    public Span<T> FirstChunk
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var reference = _reference;
            var length = (1 << reference.ChunkBitSize) - reference.Offset;
            if (_length < TIndex.CreateTruncating(length))
                length = Int32.CreateTruncating(_length);
            return MemoryMarshal.CreateSpan(ref reference.Value, length);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_reference, _length);

    public ref struct Enumerator(ChunkedReference<T, TIndex> reference, TIndex length)
    {
        private ChunkedReference<T, TIndex> _reference = reference;
        private readonly TIndex _length = length;
        private TIndex _index = TIndex.NegativeOne;
        private T _current = default!;

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _length)
                return false;

            _current = _reference.Value;
            _reference = _reference.Next();
            return true;
        }
    }

    public ref struct ChunkEnumerator(ChunkedReference<T, TIndex> reference, TIndex length)
    {
        private ChunkedReference<T, TIndex> _reference = reference;
        private readonly TIndex _length = length;
        private readonly int _chunkSize = 1 << reference.ChunkBitSize;
        private TIndex _index = TIndex.NegativeOne;

        public Span<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var reference = _reference;
                var length = _chunkSize - reference.Offset;
                if (_length < TIndex.CreateTruncating(length))
                    length = Int32.CreateTruncating(_length);
                return MemoryMarshal.CreateSpan(ref reference.Value, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var reference = _reference;
            if ((_index += TIndex.CreateTruncating(_chunkSize - reference.Offset)) >= _length)
                return false;

            _reference = reference.NextChunk();
            return true;
        }
    }
}