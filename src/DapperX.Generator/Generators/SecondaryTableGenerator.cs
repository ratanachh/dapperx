namespace DapperX.Generator.Generators;

using System.Linq;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

/// <summary>Secondary table SQL literals — primary-first INSERT, secondary-first DELETE.</summary>
internal static class SecondaryTableGenerator
{
    public static string BuildSecondaryInsertSql(EntityModel entity, SecondaryTableModel st)
    {
        var props = entity.Properties
            .Where(p => st.PropertyNames.Contains(p.PropertyName))
            .ToList();
        var idProp = entity.Properties.First(p => p.IsId);
        var cols = string.Join(", ", props.Select(p => p.ColumnName).Prepend(st.PrimaryKeyJoinColumn));
        var parms = string.Join(", ", props.Select(p => $"@{p.PropertyName}").Prepend($"@{idProp.PropertyName}"));
        return $"INSERT INTO {st.TableName} ({cols}) VALUES ({parms})";
    }

    public static string BuildSecondaryUpdateSql(EntityModel entity, SecondaryTableModel st)
    {
        var props = entity.Properties
            .Where(p => st.PropertyNames.Contains(p.PropertyName) && p.Updatable)
            .ToList();
        if (props.Count == 0)
            return $"SELECT 1 WHERE 1 = 0";

        var sets = string.Join(", ", props.Select(p => $"{p.ColumnName} = @{p.PropertyName}"));
        return $"UPDATE {st.TableName} SET {sets} WHERE {st.PrimaryKeyJoinColumn} = @{entity.Properties.First(p => p.IsId).PropertyName}";
    }

    public static string BuildSecondaryDeleteSql(SecondaryTableModel st)
        => $"DELETE FROM {st.TableName} WHERE {st.PrimaryKeyJoinColumn} = @id";

    public static string BuildSecondaryDeleteByIdsSql(SecondaryTableModel st, string provider)
        => $"DELETE FROM {st.TableName} WHERE {ProviderSqlHelper.InClause(st.PrimaryKeyJoinColumn, "ids", provider)}";

    public static string SanitizeTableKey(string tableName)
        => string.Concat(tableName.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
}
