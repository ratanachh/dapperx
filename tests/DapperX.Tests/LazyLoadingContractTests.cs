using DapperX.Relations.Lazy;
using System.Reflection;

namespace DapperX.Tests;

public class LazyLoadingContractTests
{
    [Fact]
    public void LazyCollection_and_LazyReference_have_no_Reload_method()
    {
        Assert.Null(typeof(LazyCollection<string>).GetMethod("Reload", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(LazyReference<string>).GetMethod("Reload", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(LazyMap<int, string>).GetMethod("Reload", BindingFlags.Public | BindingFlags.Instance));
    }
}
