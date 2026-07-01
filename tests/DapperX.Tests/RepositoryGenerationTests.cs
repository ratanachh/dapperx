using System.Reflection;
using DapperX.Runtime.Repositories;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class RepositoryGenerationTests
{
    [Fact]
    public void ProductRepositoryImpl_extends_DapperXRepositoryBase_and_implements_interface()
    {
        var impl = typeof(ProductRepositoryImpl);
        Assert.True(typeof(DapperXRepositoryBase<Product, int>).IsAssignableFrom(impl));
        Assert.True(typeof(IProductRepository).IsAssignableFrom(impl));
        Assert.True(impl.IsSealed);
        Assert.False(impl.IsAbstract);
    }

    [Fact]
    public void ProductRepositoryImpl_overrides_lifecycle_hooks_when_entity_declares_them()
    {
        AssertLifecycleOverride("OnPrePersist");
        AssertLifecycleOverride("OnPostPersist");
        AssertLifecycleOverride("OnPostLoad");
    }

    private static void AssertLifecycleOverride(string methodName)
    {
        var method = typeof(ProductRepositoryImpl).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.True(method!.IsFamily);
        Assert.Equal(typeof(void), method.ReturnType);
        Assert.True(method.IsVirtual);
        Assert.False(method.IsAbstract);
    }

    [Fact]
    public void ProductLifecycleInvoker_is_generated()
        => Assert.NotNull(typeof(ProductLifecycleInvoker));

    [Fact]
    public void ProductRepositoryImpl_declares_FindByNameAsync()
    {
        var method = typeof(ProductRepositoryImpl).GetMethod(
            nameof(IProductRepository.FindByNameAsync),
            [typeof(string)]);
        Assert.NotNull(method);
        Assert.True(method!.IsPublic);
    }
}
