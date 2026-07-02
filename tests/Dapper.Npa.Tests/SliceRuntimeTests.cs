using Dapper.Npa.Abstractions.Paging;

namespace Dapper.Npa.Tests;

public class SliceRuntimeTests
{
    [Fact]
    public void Slice_HasNext_true_when_extra_row_fetched()
    {
        var rows = new[] { 1, 2, 3 };
        var slice = new Slice<int>(rows, pageSize: 2);
        Assert.True(slice.HasNext);
        Assert.Equal(2, slice.Content.Count);
    }

    [Fact]
    public void Slice_HasNext_false_when_count_equals_page_size()
    {
        var rows = new[] { 1, 2 };
        var slice = new Slice<int>(rows, pageSize: 2);
        Assert.False(slice.HasNext);
        Assert.Equal(2, slice.Content.Count);
    }

    [Fact]
    public void Slice_HasNext_false_when_under_page_size()
    {
        var rows = new[] { 1 };
        var slice = new Slice<int>(rows, pageSize: 5);
        Assert.False(slice.HasNext);
        Assert.Single(slice.Content);
    }
}
