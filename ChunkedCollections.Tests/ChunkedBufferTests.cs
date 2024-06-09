using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChunkedCollections.Tests;

using TestChunkedBuffer = ChunkedBuffer<int, int>;

[TestClass]
public class ChunkedBufferTests
{
    [TestMethod]
    public void ChunkedBuffer_Alloate()
    {
        var buffer = TestChunkedBuffer.Allocate(0, 4);
        Assert.AreEqual(0, buffer.Length);

        buffer = TestChunkedBuffer.Allocate(3, 4);
        Assert.AreEqual(3, buffer.Length);

        buffer = TestChunkedBuffer.Allocate(6, 4);
        Assert.AreEqual(6, buffer.Length);
    }
}
