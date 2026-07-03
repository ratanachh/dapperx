using DapperX.Generator.Models;

namespace DapperX.Generator.Builders;

using Generator.Models;

/// <summary>Compile-time INSERT/UPDATE fragments for auditing columns.</summary>
internal static class AuditingSqlBuilder
{
    internal readonly record struct SqlAssignment(string Column, string ValueExpression);

    public static IReadOnlyList<SqlAssignment> GetInsertAssignments(
        EntityModel entity,
        IReadOnlyCollection<string> existingColumns,
        string provider)
    {
        if (entity.Auditing is null)
            return [];

        var list = new List<SqlAssignment>();
        var auditing = entity.Auditing;

        AddIfMissing(list, entity, auditing.CreatedDateProperty, existingColumns, useTimestamp: true, provider);
        AddIfMissing(list, entity, auditing.CreatedByProperty, existingColumns, useTimestamp: false, provider);
        AddIfMissing(list, entity, auditing.LastModifiedDateProperty, existingColumns, useTimestamp: true, provider);
        AddIfMissing(list, entity, auditing.LastModifiedByProperty, existingColumns, useTimestamp: false, provider);

        return list;
    }

    public static IReadOnlyList<SqlAssignment> GetUpdateAssignments(
        EntityModel entity,
        IReadOnlyCollection<string> existingSetColumns,
        string provider)
    {
        if (entity.Auditing is null)
            return [];

        var list = new List<SqlAssignment>();
        var auditing = entity.Auditing;

        AddIfMissing(list, entity, auditing.LastModifiedDateProperty, existingSetColumns, useTimestamp: true, provider);
        AddIfMissing(list, entity, auditing.LastModifiedByProperty, existingSetColumns, useTimestamp: false, provider);

        return list;
    }

    internal static string CurrentTimestampLiteral(string provider)
        => provider switch
        {
            "SqlServer" => "GETDATE()",
            "PostgreSql" => "CURRENT_TIMESTAMP",
            "MySql" => "NOW()",
            "Sqlite" => "datetime('now')",
            _ => "GETDATE()",
        };

    private static void AddIfMissing(
        List<SqlAssignment> list,
        EntityModel entity,
        string? propertyName,
        IReadOnlyCollection<string> existingColumns,
        bool useTimestamp,
        string provider)
    {
        if (propertyName is null)
            return;

        var prop = entity.Properties.FirstOrDefault(p => p.PropertyName == propertyName);
        if (prop is null)
            return;

        if (existingColumns.Contains(prop.ColumnName))
            return;

        var value = useTimestamp
            ? CurrentTimestampLiteral(provider)
            : $"@{prop.PropertyName}";
        list.Add(new SqlAssignment(prop.ColumnName, value));
    }
}
