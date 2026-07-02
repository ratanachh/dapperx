namespace Dapper.Npa.Batching.Batch;

public static class BatchChunker
{
    public static IEnumerable<IReadOnlyList<T>> Chunk<T>(IEnumerable<T> source, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), chunkSize, "Chunk size must be greater than 0.");

        var list = new List<T>(chunkSize);
        foreach (var item in source)
        {
            list.Add(item);
            if (list.Count == chunkSize)
            {
                yield return list.AsReadOnly();
                list = new List<T>(chunkSize);
            }
        }
        if (list.Count > 0)
            yield return list.AsReadOnly();
    }
}
