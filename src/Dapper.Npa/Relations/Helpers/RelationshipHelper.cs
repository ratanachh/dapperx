namespace Dapper.Npa.Relations.Helpers;
public static class RelationshipHelper
{
    public static string BuildFkInSql(string table, string fkColumn, string whereBase = "")
        => $"SELECT * FROM {table} WHERE {fkColumn} IN @parentIds{(string.IsNullOrEmpty(whereBase) ? string.Empty : " AND " + whereBase)}";
}
