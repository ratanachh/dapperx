using Dapper.Npa.Query.Query;

namespace Dapper.Npa.Tests;

public class QueryBuilderTests
{
    [Fact]
    public void QueryBuilder_tracks_include_and_split_query_flags()
    {
        var state = new QueryBuilder<ProductStub>()
            .Where(p => p.Name == "x")
            .Include("Customer")
            .AsSplitQuery()
            .IncludeDeleted()
            .Build();

        Assert.Single(state.Includes);
        Assert.Equal("Customer", state.Includes[0]);
        Assert.True(state.SplitQuery);
        Assert.True(state.IncludeDeleted);
    }

    private sealed class ProductStub
    {
        public string Name { get; set; } = string.Empty;
    }
}
