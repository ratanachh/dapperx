using Dapper.Npa.Batching.Batch;

namespace Dapper.Npa.Tests;

public class BatchChunkerTests
{
    [Fact]
    public void Chunk_preserves_order_and_sizes()
    {
        var source = Enumerable.Range(1, 7).ToArray();
        var chunks = BatchChunker.Chunk(source, 3).ToList();

        Assert.Equal(3, chunks.Count);
        Assert.Equal(new[] { 1, 2, 3 }, chunks[0]);
        Assert.Equal(new[] { 4, 5, 6 }, chunks[1]);
        Assert.Equal(new[] { 7 }, chunks[2]);
    }

    [Fact]
    public void Chunk_throws_when_chunk_size_invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BatchChunker.Chunk(new[] { 1, 2 }, 0).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => BatchChunker.Chunk(new[] { 1, 2 }, -4).ToList());
    }

    [Fact]
    public void Chunk_throws_when_source_null()
    {
        IEnumerable<int>? source = null;
        Assert.Throws<ArgumentNullException>(() => BatchChunker.Chunk(source!, 2).ToList());
    }
}
