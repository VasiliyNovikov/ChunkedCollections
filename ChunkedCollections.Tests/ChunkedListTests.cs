using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ChunkedCollections.Tests;

using TestChunkedList = ChunkedList<int>;

[TestClass]
public class ChunkedListTests
{
    [TestMethod]
    public void Add_SingleItem_CountIncreases()
    {
        var list = new ChunkedList<int>(4);
        list.Add(5);
        Assert.AreEqual(1, list.Count);
    }

    [TestMethod]
    public void Indexer_GetSet_CorrectValueReturned()
    {
        var list = new ChunkedList<int>(4);
        list.Add(10);
        Assert.AreEqual(10, list[0]);
        list[0] = 20;
        Assert.AreEqual(20, list[0]);
    }

    [TestMethod]
    public void GetEnumerator_MoveNext_ReturnsTrueAndFalseCorrectly()
    {
        var list = new ChunkedList<int>(4);
        list.Add(1);
        list.Add(2);

        var enumerator = list.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(2, enumerator.Current);
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void Clear_EmptiesList_CountIsZero()
    {
        var list = new ChunkedList<int>(4);
        list.Add(1);
        list.Clear();

        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Sort_OrdersItems_CorrectOrder()
    {
        var list = new ChunkedList<int>(4);
        list.Add(5);
        list.Add(1);
        list.Add(4);
        list.MergeSort();

        Assert.AreEqual(1, list[0]);
        Assert.AreEqual(4, list[1]);
        Assert.AreEqual(5, list[2]);
    }

    [TestMethod]
    public void MergeSort_RandomNumbers_SortedCorrectly()
    {
        const int initialCount = 100_000;
        const int chunkBitSize = 5;
        for (var count = initialCount; count < initialCount + (1 << chunkBitSize); ++count)
        {
            var list = new ChunkedList<int>(5);
            var random = new Random();

            for (var i = 0; i < count; ++i)
                list.Add(random.Next(0, 10_000));

            list.MergeSort();

            for (var i = 1; i < list.Count; ++i)
                Assert.IsTrue(list[i - 1] <= list[i]);
        }
    }

    [TestMethod]
    public void QuickSort_RandomNumbers_SortedCorrectly()
    {
        const int initialCount = 100_000;
        const int chunkBitSize = 5;
        for (var count = initialCount; count < initialCount + (1 << chunkBitSize); ++count)
        {
            var list = new ChunkedList<int>(5);
            var random = new Random();

            for (var i = 0; i < count; ++i)
                list.Add(random.Next(0, 10_000));

            list.QuickSort();

            for (var i = 1; i < list.Count; ++i)
                Assert.IsTrue(list[i - 1] <= list[i]);
        }
    }
}