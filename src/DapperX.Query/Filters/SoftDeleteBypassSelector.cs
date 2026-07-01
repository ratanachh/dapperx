namespace DapperX.Query.Filters;
public static class SoftDeleteBypassSelector
{
    /// <summary>Selects between the with-filter SQL (default) and without-filter SQL (IncludeDeleted).</summary>
    public static string Select(string withFilterSql, string? withoutFilterSql, bool includeDeleted)
        => includeDeleted && withoutFilterSql is not null ? withoutFilterSql : withFilterSql;
}
