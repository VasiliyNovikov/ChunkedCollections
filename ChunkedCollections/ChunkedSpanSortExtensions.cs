using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ChunkedCollections;

public static class ChunkedSpanSortExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Sort<T, TIndex>(this ChunkedSpan<T, TIndex> span)
        where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
        where T : IComparisonOperators<T, T, bool>
    {
        var length = span.Length;
        if (length <= TIndex.One)
            return;

        if (span.IsSingleChunk)
        {
            span.FirstChunk.Sort();
            return;
        }

        if (length == TIndex.CreateTruncating(2))
        {
            var firstRef = span.Start;
            var secondRef = firstRef.Next();
            SwapIfLessThan(ref firstRef.Value, ref secondRef.Value);
            return;
        }

        if (length == TIndex.CreateTruncating(3))
        {
            var firstRef = span.Start;
            var secondRef = firstRef.Next();
            var thirdRef = secondRef.Next();
            ref var first = ref firstRef.Value;
            ref var second = ref secondRef.Value;
            ref var third = ref thirdRef.Value;
            SwapIfLessThan(ref second, ref first);
            SwapIfLessThan(ref third, ref first);
            SwapIfLessThan(ref third, ref second);
            return;
        }

        if (length < TIndex.CreateTruncating(16))
        {
            InsertionSort(span);
            return;
        }

        QuickSort(span);
        return;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InsertionSort<T, TIndex>(ChunkedSpan<T, TIndex> span)
        where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
        where T : IComparisonOperators<T, T, bool>
    {
        var iRef = span.Start;
        var iRefNext = iRef.Next();
        var lastIndex = span.Length - TIndex.One;
        for (var i = TIndex.Zero; i < lastIndex; ++i)
        {
            T t = iRefNext.Value;

            var j = i;
            var jRef = iRef;
            var jRefNext = jRef.Next();
            while (j >= TIndex.Zero && t < jRef.Value)
            {
                jRefNext.Value = jRef.Value;
                --j;
                jRefNext = jRef;
                jRef = jRef.Prev();
            }

            jRefNext.Value = t;

            iRef = iRefNext;
            iRefNext = iRef.Next();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void QuickSort<T, TIndex>(ChunkedSpan<T, TIndex> span)
        where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
        where T : IComparisonOperators<T, T, bool>
    {
        var length = span.Length;
        var lastIndex = length - TIndex.One;
        var middleIndex = length >> 1;

        var startRef = span.Start;

        ref var start = ref startRef.Value;
        ref var middle = ref span[middleIndex];
        ref var pivot = ref span[lastIndex];

        SwapIfLessThan(ref pivot, ref start);
        SwapIfLessThan(ref middle, ref start);
        SwapIfLessThan(ref middle, ref pivot);

        var i = TIndex.Zero;
        var iRef = startRef;
        var jRef = startRef;
        for (var j = TIndex.Zero; j < lastIndex; ++j)
        {
            ref var jItem = ref jRef.Value;
            if (jItem < pivot)
            {
                Swap(ref iRef.Value, ref jItem);
                ++i;
                iRef = iRef.Next();
            }
            jRef = jRef.Next();
        }

        Swap(ref iRef.Value, ref pivot);

        Sort(span.Slice(TIndex.Zero, i));
        Sort(span.Slice(i + TIndex.One));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SwapIfLessThan<T>(ref T v1, ref T v2) where T : IComparisonOperators<T, T, bool>
    {
        if (v1 < v2)
            Swap(ref v1, ref v2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap<T>(ref T v1, ref T v2) => (v1, v2) = (v2, v1);
}