namespace Dapper.Npa.Abstractions.Lifecycle;

public interface ILifecycleInvoker<T>
{
    void InvokePrePersist(T entity);
    void InvokePostPersist(T entity);
    void InvokePreUpdate(T entity);
    void InvokePostUpdate(T entity);
    void InvokePreRemove(T entity);
    void InvokePostRemove(T entity);
    void InvokePostLoad(T entity);
}
