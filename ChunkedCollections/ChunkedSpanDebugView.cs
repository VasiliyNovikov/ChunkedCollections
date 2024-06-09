using System;
using System.Diagnostics;
using System.Numerics;

namespace ChunkedCollections;

internal class ChunkedSpanDebugView<T, TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items { get; }

    public ChunkedSpanDebugView(ChunkedSpan<T, TIndex> span)
    {
        var length = Int32.CreateChecked(span.Length);
        Items = new T[length];
        for (var i = 0; i < length; ++i)
            Items[i] = span[TIndex.CreateTruncating(i)];
    }
}