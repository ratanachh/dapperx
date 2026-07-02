using Dapper.Npa.Runtime.Configuration;

namespace Dapper.Npa.Tests;

public class DapperXOptionsTests
{
    [Fact]
    public void EnableFilter_makes_IsFilterActive_true()
    {
        var options = new DapperXOptions();
        options.EnableFilter("tenant", new { tenantId = 42 });

        Assert.True(options.IsFilterActive("tenant"));
        Assert.Equal(42, ((dynamic)options.GetFilterParameters("tenant")!).tenantId);
    }

    [Fact]
    public void DisableFilter_removes_active_filter()
    {
        var options = new DapperXOptions();
        options.EnableFilter("status");
        options.DisableFilter("status");

        Assert.False(options.IsFilterActive("status"));
        Assert.Null(options.GetFilterParameters("status"));
    }

    [Fact]
    public void IsFilterActive_is_false_for_unknown_filter()
        => Assert.False(new DapperXOptions().IsFilterActive("missing"));

    [Fact]
    public void EnableFilter_overwrites_parameters_for_same_name()
    {
        var options = new DapperXOptions();
        options.EnableFilter("region", "EU");
        options.EnableFilter("region", "US");

        Assert.Equal("US", options.GetFilterParameters("region"));
    }

    [Fact]
    public void Concurrent_enable_and_disable_remain_consistent()
    {
        var options = new DapperXOptions();
        var names = Enumerable.Range(0, 32).Select(i => $"filter_{i}").ToArray();

        Parallel.ForEach(names, name =>
        {
            options.EnableFilter(name);
            Assert.True(options.IsFilterActive(name));
            options.DisableFilter(name);
        });

        foreach (var name in names)
            Assert.False(options.IsFilterActive(name));
    }
}
