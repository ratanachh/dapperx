using System.Data;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests;

public class LazyLoadingTests
{
    private sealed class MapItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private sealed class StringMapItem
    {
        public string Key { get; init; } = string.Empty;
        public int Value { get; init; }
    }

    [Fact]
    public async Task LazyCollection_GetAsync_loads_once_under_concurrency()
    {
        var calls = 0;
        var lazy = new LazyCollection<string>(async (_, _) =>
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(20);
            return new[] { "a", "b" };
        });

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => lazy.GetAsync((IDbConnection)null!))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, calls);
        Assert.True(lazy.IsLoaded);
        Assert.NotNull(lazy.TryGet());
        Assert.Equal(2, lazy.TryGet()!.Count);
    }

    [Fact]
    public async Task LazyMap_GetAsync_loads_once_under_concurrency_and_groups_by_key()
    {
        var calls = 0;
        var lazy = new LazyMap<int, MapItem>(
            x => x.Id,
            async (_, _) =>
            {
                Interlocked.Increment(ref calls);
                await Task.Delay(20);
                return new[]
                {
                    new MapItem { Id = 1, Name = "one" },
                    new MapItem { Id = 2, Name = "two" },
                };
            });

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => lazy.GetAsync((IDbConnection)null!))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, calls);
        var map = lazy.TryGet();
        Assert.NotNull(map);
        Assert.Equal("one", map![1].Name);
        Assert.Equal("two", map[2].Name);
    }

    [Fact]
    public void LazyCollection_and_LazyMap_Set_and_TryGet_never_trigger_loader()
    {
        var collectionCalls = 0;
        var collection = new LazyCollection<string>(async (_, _) =>
        {
            Interlocked.Increment(ref collectionCalls);
            return await Task.FromResult<IEnumerable<string>>(new[] { "1" });
        });

        collection.Set(new[] { "3", "4" });
        Assert.True(collection.IsLoaded);
        Assert.Equal(new[] { "3", "4" }, collection.TryGet());
        Assert.Equal(0, collectionCalls);

        var mapCalls = 0;
        var map = new LazyMap<string, StringMapItem>(
            x => x.Key,
            async (_, _) =>
            {
                Interlocked.Increment(ref mapCalls);
                return await Task.FromResult<IEnumerable<StringMapItem>>(new[]
                {
                    new StringMapItem { Key = "a", Value = 1 },
                });
            });

        map.Set(new Dictionary<string, StringMapItem>
        {
            ["x"] = new StringMapItem { Key = "x", Value = 10 },
        });

        Assert.True(map.IsLoaded);
        Assert.Equal(10, map.TryGet()!["x"].Value);
        Assert.Equal(0, mapCalls);
    }

    [Fact]
    public async Task LazyReference_GetAsync_loads_once_under_concurrency()
    {
        var calls = 0;
        var lazy = new LazyReference<string>(async (_, _) =>
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(20);
            return "loaded";
        });

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => lazy.GetAsync((IDbConnection)null!))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, calls);
        Assert.True(lazy.IsLoaded);
        Assert.All(results, r => Assert.Equal("loaded", r));
    }
}
