namespace Dapper.Npa.Core.Attributes;

/// <summary>Native SQL expression included verbatim in SELECT — never in INSERT/UPDATE/WHERE.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class FormulaAttribute(string sql) : Attribute
{
    public string Sql { get; } = sql;
}
