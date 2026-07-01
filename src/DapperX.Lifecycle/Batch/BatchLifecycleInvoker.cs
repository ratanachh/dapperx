namespace DapperX.Lifecycle.Batch;
public abstract class BatchLifecycleInvoker<T> where T : class
{
    public virtual void InvokePrePersistBatch(IEnumerable<T> entities) { }
    public virtual void InvokePostPersistBatch(IEnumerable<T> entities) { }
    public virtual void InvokePreUpdateBatch(IEnumerable<T> entities) { }
    public virtual void InvokePostUpdateBatch(IEnumerable<T> entities) { }
    public virtual void InvokePreRemoveBatch(IEnumerable<T> entities) { }
    public virtual void InvokePostRemoveBatch(IEnumerable<T> entities) { }
}
