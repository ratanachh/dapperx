namespace Dapper.Npa.Lifecycle.Invokers;
public abstract class LifecycleInvokerBase<T> where T : class
{
    public virtual void InvokePrePersist(T entity) { }
    public virtual void InvokePostPersist(T entity) { }
    public virtual void InvokePreUpdate(T entity) { }
    public virtual void InvokePostUpdate(T entity) { }
    public virtual void InvokePreRemove(T entity) { }
    public virtual void InvokePostRemove(T entity) { }
    public virtual void InvokePostLoad(T entity) { }
}
