namespace DapperX.Generator.Generators;

using System.Linq;
using System.Text;
using DapperX.Generator.Builders;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

internal static class BulkInsertGenerator
{
    internal sealed record BulkInsertColumn(string PropertyName, string ColumnName, string ClrTypeName);

    public static IReadOnlyList<BulkInsertColumn> GetInsertColumns(EntityModel entity)
    {
        return entity.Properties
            .Where(p => !p.IsTransient && p.Insertable && p.Formula is null && p.ColumnTransformer?.Write is null
                        && p.GeneratedTime is null && p.SecondaryTable is null
                        && !(p.IsId && p.IdGenerationStrategy == "Identity"))
            .Select(p => new BulkInsertColumn(p.PropertyName, p.ColumnName, p.ClrTypeName))
            .ToList();
    }

    public static void EmitMetadata(StringBuilder sb, EntityModel entity)
    {
        var columns = GetInsertColumns(entity);
        if (columns.Count == 0)
            return;

        var table = SqlBuilder.FullTable(entity);
        sb.AppendLine($"    private const string BulkInsertTableName = \"{Escape(table)}\";");
        sb.AppendLine("    private static readonly string[] BulkInsertColumnNames =");
        sb.AppendLine("    {");
        foreach (var column in columns)
            sb.AppendLine($"        \"{Escape(column.ColumnName)}\",");
        sb.AppendLine("    };");
        sb.AppendLine("    private static readonly Type[] BulkInsertColumnTypes =");
        sb.AppendLine("    {");
        foreach (var column in columns)
            sb.AppendLine($"        typeof({column.ClrTypeName}),");
        sb.AppendLine("    };");
        sb.AppendLine();
    }

    public static void EmitRowBuilderMethod(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        var columns = GetInsertColumns(entity);
        if (columns.Count == 0)
            return;

        sb.AppendLine($"    private static object?[] BuildBulkInsertRow({entityFqn} entity) =>");
        sb.AppendLine("        new object?[]");
        sb.AppendLine("        {");
        foreach (var column in columns)
            sb.AppendLine($"            entity.{column.PropertyName},");
        sb.AppendLine("        };");
        sb.AppendLine();
    }

    public static string GetExecutorTypeName(string provider)
        => provider switch
        {
            "SqlServer" => "global::DapperX.Provider.SqlServer.SqlServerBulkExecutor",
            "PostgreSql" => "global::DapperX.Provider.PostgreSql.PostgreSqlBulkExecutor",
            "MySql" => "global::DapperX.Provider.MySql.MySqlBatchExecutor",
            _ => string.Empty,
        };

    private static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
