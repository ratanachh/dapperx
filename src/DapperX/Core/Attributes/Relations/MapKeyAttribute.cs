namespace DapperX.Core.Attributes;

/// <summary>Specifies the key column for LazyMap&lt;TKey,TValue&gt; relationships.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class MapKeyAttribute(string column) : Attribute
{
    public string Column { get; } = column;
}
