namespace Dapper.Npa.Tests;

public class LifecycleTests
{
    private static string ReadGenerated(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            fileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProductLifecycleInvoker_emits_entity_hooks()
    {
        var source = ReadGenerated("ProductLifecycleInvoker.g.cs");
        Assert.Contains("InvokePrePersist", source);
        Assert.Contains("InvokePostPersist", source);
        Assert.Contains("InvokePostLoad", source);
        Assert.Contains("entity.OnPrePersist()", source);
        Assert.Contains("entity.OnPostLoad()", source);
    }

    [Fact]
    public void ListenerOnlyItemLifecycleInvoker_emits_direct_listener_calls_without_reflection()
    {
        var source = ReadGenerated("ListenerOnlyItemLifecycleInvoker.g.cs");
        Assert.Contains("AuditListener _entityListener0", source);
        Assert.Contains("_entityListener0.BeforeInsert(entity)", source);
        Assert.Contains("_entityListener0.AfterLoad(entity)", source);
        Assert.DoesNotContain("GetMethod", source);
        Assert.DoesNotContain("Invoke(", source);
    }

    [Fact]
    public void ListenerOnlyItemRepositoryImpl_overrides_hooks_for_listener_only_entity()
    {
        var source = ReadGenerated("ListenerOnlyItemRepositoryImpl.g.cs");
        Assert.Contains("ListenerOnlyItemLifecycleInvoker _lifecycle", source);
        Assert.Contains("protected override void OnPrePersist", source);
        Assert.Contains("protected override void OnPostLoad", source);
        Assert.Contains("_lifecycle.InvokePrePersist(entity)", source);
        Assert.Contains("_lifecycle.InvokePostLoad(entity)", source);
    }

    [Fact]
    public void BatchLifecycleItemRepositoryImpl_delete_many_invokes_pre_batch_before_loop()
    {
        var source = ReadGenerated("BatchLifecycleItemRepositoryImpl.g.cs");
        var start = source.IndexOf("public override async Task DeleteManyAsync", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = source.IndexOf("public override async Task DeleteAllByIdAsync", start, StringComparison.Ordinal);
        var body = source[start..end];
        var preBatch = body.IndexOf("InvokePreRemoveBatch(list)", StringComparison.Ordinal);
        var loop = body.IndexOf("foreach (var chunk", StringComparison.Ordinal);
        Assert.True(preBatch >= 0 && loop > preBatch);
    }

    [Fact]
    public void SharedListenerItemLifecycleInvoker_reuses_shared_listener_type()
    {
        var listenerOnly = ReadGenerated("ListenerOnlyItemLifecycleInvoker.g.cs");
        var shared = ReadGenerated("SharedListenerItemLifecycleInvoker.g.cs");
        Assert.Contains("AuditListener _entityListener0", listenerOnly);
        Assert.Contains("AuditListener _entityListener0", shared);
        Assert.Contains("_entityListener0.BeforeInsert(entity)", shared);
    }

    [Fact]
    public void BatchLifecycleItemRepositoryImpl_emits_batch_hook_wrapping_order()
    {
        var source = ReadGenerated("BatchLifecycleItemRepositoryImpl.g.cs");
        Assert.Contains("BatchLifecycleInvoker _batchLifecycle", source);
        Assert.Contains("BatchLifecycleItemBatchLifecycleInvoker", source);

        var insertManyStart = source.IndexOf("InsertManyAsync", StringComparison.Ordinal);
        var insertManyEnd = source.IndexOf("UpdateManyAsync", StringComparison.Ordinal);
        Assert.True(insertManyStart >= 0 && insertManyEnd > insertManyStart);
        var insertMany = source.Substring(insertManyStart, insertManyEnd - insertManyStart);
        Assert.True(insertMany.IndexOf("InvokePrePersistBatch", StringComparison.Ordinal)
            < insertMany.IndexOf("OnPrePersist(entity)", StringComparison.Ordinal));
        Assert.True(insertMany.IndexOf("OnPostPersist(entity)", StringComparison.Ordinal)
            < insertMany.IndexOf("InvokePostPersistBatch", StringComparison.Ordinal));

        var updateManyStart = source.IndexOf("UpdateManyAsync", StringComparison.Ordinal);
        var updateManyEnd = source.IndexOf("DeleteManyAsync", StringComparison.Ordinal);
        Assert.True(updateManyStart >= 0 && updateManyEnd > updateManyStart);
        var updateMany = source.Substring(updateManyStart, updateManyEnd - updateManyStart);
        Assert.Contains("InvokePreUpdateBatch", updateMany);
        Assert.Contains("InvokePostUpdateBatch", updateMany);

        var deleteManyStart = source.IndexOf("DeleteManyAsync", StringComparison.Ordinal);
        var deleteMany = source.Substring(deleteManyStart);
        Assert.Contains("InvokePreRemoveBatch", deleteMany);
        Assert.Contains("InvokePostRemoveBatch", deleteMany);
    }

    [Fact]
    public void ProductRepositoryImpl_native_query_emits_post_load()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        var nativeStart = source.IndexOf("FindByNameNativeAsync", StringComparison.Ordinal);
        Assert.True(nativeStart >= 0);
        var nativeBody = source.Substring(nativeStart, Math.Min(500, source.Length - nativeStart));
        Assert.Contains("QueryAsync", nativeBody);
        Assert.Contains("OnPostLoad(__e)", nativeBody);
    }

    [Fact]
    public void OrderRepositoryImpl_OnPostLoad_invokes_lifecycle_before_lazy_wiring()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");
        var postLoadStart = source.IndexOf("protected override void OnPostLoad", StringComparison.Ordinal);
        Assert.True(postLoadStart >= 0);
        var postLoadEnd = source.IndexOf("public override async Task<Dapper.Npa.Tests.Fixtures.Order?> GetByIdAsync", postLoadStart, StringComparison.Ordinal);
        var body = source.Substring(postLoadStart, postLoadEnd - postLoadStart);
        Assert.Contains("_lifecycle.InvokePostLoad(entity)", body);
        Assert.Contains("WireLazyLoaders(entity)", body);
        Assert.True(body.IndexOf("InvokePostLoad", StringComparison.Ordinal)
            < body.IndexOf("WireLazyLoaders", StringComparison.Ordinal));
    }
}
