namespace DapperX.Core.Attributes;

/// <summary>
/// Marks an interface for DapperX repository generation.
/// The entity type is inferred from the IRepository&lt;TEntity, TId&gt; generic argument.
/// Generator emits a sealed {Name}RepositoryImpl class implementing this interface.
/// Naming convention: strip leading 'I', append 'Impl'.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class RepositoryAttribute : Attribute { }
