using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ChunkedCollections;

internal static class ChunkedCollectionHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckIndexInRange<TIndex>(TIndex index, TIndex length)
        where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
    {
        if (!IsIndexInRange(index, length))
            throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIndexInRange<TIndex>(TIndex index, TIndex length)
        where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>
    {
        if (typeof(TIndex) == typeof(Int128))
            return (UInt128)(Int128)(object)index >= (UInt128)(Int128)(object)length;
        else if (typeof(TIndex) == typeof(long))
            return (ulong)(long)(object)index >= (ulong)(long)(object)length;
        else if (typeof(TIndex) == typeof(int))
            return (uint)(int)(object)index >= (uint)(int)(object)length;
        else if (typeof(TIndex) == typeof(short))
            return (ushort)(short)(object)index >= (ushort)(short)(object)length;
        else if (typeof(TIndex) == typeof(sbyte))
            return (byte)(sbyte)(object)index >= (byte)(sbyte)(object)length;
        else
            return index >= TIndex.Zero && index < length; 
    }
}