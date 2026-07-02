namespace Dapper.Npa.Generator.Models;

internal enum DerivedQueryPathKind
{
    /// <summary>Direct entity column (e.g. Name → name).</summary>
    Direct,
    /// <summary>Flattened [Embedded] column (e.g. AddressCity → address_city).</summary>
    Embedded,
    /// <summary>FK column on owning entity (e.g. CustomerId → customer_id).</summary>
    NavigationForeignKey,
    /// <summary>Column on related entity via single-level JOIN (e.g. CustomerName).</summary>
    NavigationJoin,
}

internal sealed class DerivedQueryPathModel
{
    /// <summary>Method-name path token (PascalCase concatenation).</summary>
    public string PathKey { get; init; } = string.Empty;
    public DerivedQueryPathKind Kind { get; init; }
    /// <summary>Qualified column for predicates/ORDER BY (e.g. e.name, nav_Customer.name).</summary>
    public string ColumnExpression { get; init; } = string.Empty;
    public string? JoinAlias { get; init; }
    public string? JoinTable { get; init; }
    /// <summary>Compile-time JOIN ON clause without JOIN keyword.</summary>
    public string? JoinOnSql { get; init; }
    public bool IsSortable { get; init; }
}
