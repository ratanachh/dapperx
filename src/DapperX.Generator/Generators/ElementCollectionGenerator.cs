namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Models;

internal static class ElementCollectionGenerator
{
    public static void Emit(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        if (entity.ElementCollections.Count == 0)
            return;

        foreach (var ec in entity.ElementCollections)
        {
            EmitSqlConstants(sb, ec);
        }

        sb.AppendLine();

        foreach (var ec in entity.ElementCollections)
        {
            EmitLoadMethod(sb, entity, entityFqn, idType, ec);
            EmitInsertMethod(sb, entity, entityFqn, idType, ec);
            EmitDeleteMethod(sb, entity, entityFqn, idType, ec);
        }
    }

    private static void EmitSqlConstants(StringBuilder sb, ElementCollectionModel ec)
    {
        sb.AppendLine($"    private const string Load{ec.PropertyName}Sql = \"{Esc(BuildSelectSql(ec))}\";");
        sb.AppendLine($"    private const string Insert{ec.PropertyName}Sql = \"{Esc(BuildInsertSql(ec))}\";");
        sb.AppendLine($"    private const string Delete{ec.PropertyName}Sql = \"{Esc(BuildDeleteSql(ec))}\";");
    }

    private static void EmitLoadMethod(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string idType,
        ElementCollectionModel ec)
    {
        var elementType = ec.ElementTypeName;
        sb.AppendLine($"    public async Task<IReadOnlyList<{elementType}>> Load{ec.PropertyName}Async({entityFqn} parent, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"Load{ec.PropertyName}Async\";");
        sb.AppendLine($"        var parentId = parent.{entity.Properties.First(p => p.IsId).PropertyName};");
        sb.AppendLine($"        var rows = await DbExecutor.QueryAsync<{elementType}>(_connection, Load{ec.PropertyName}Sql, new {{ parentId }}, transaction);");
        sb.AppendLine("        return rows.ToList();");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitInsertMethod(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string idType,
        ElementCollectionModel ec)
    {
        var elementType = ec.ElementTypeName;
        sb.AppendLine($"    public async Task Insert{ec.PropertyName}Async({entityFqn} parent, IEnumerable<{elementType}> values, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"Insert{ec.PropertyName}Async\";");
        sb.AppendLine($"        var parentId = parent.{entity.Properties.First(p => p.IsId).PropertyName};");
        sb.AppendLine("        var list = values.ToList();");
        sb.AppendLine("        if (list.Count == 0) return;");

        var orderParam = ec.OrderColumnName is not null ? SanitizeParam(ec.OrderColumnName) : null;

        if (ec.IsEmbeddable)
        {
            var assignments = new List<string> { "parentId" };
            for (var i = 0; i < ec.ValuePropertyNames.Count; i++)
            {
                var colParam = SanitizeParam(ec.ValueColumns[i]);
                assignments.Add($"{colParam} = value.{ec.ValuePropertyNames[i]}");
            }

            if (orderParam is not null)
                assignments.Add($"{orderParam} = index");

            sb.AppendLine($"        var rows = list.Select((value, index) => new {{ {string.Join(", ", assignments)} }});");
        }
        else if (ec.ValueColumns.Count == 1)
        {
            var colParam = SanitizeParam(ec.ValueColumns[0]);
            if (orderParam is not null)
                sb.AppendLine($"        var rows = list.Select((value, index) => new {{ parentId, {colParam} = value, {orderParam} = index }});");
            else
                sb.AppendLine($"        var rows = list.Select(value => new {{ parentId, {colParam} = value }});");
        }
        else
        {
            var assignments = new List<string> { "parentId" };
            for (var i = 0; i < ec.ValuePropertyNames.Count; i++)
            {
                var colParam = SanitizeParam(ec.ValueColumns[i]);
                assignments.Add($"{colParam} = value.{ec.ValuePropertyNames[i]}");
            }

            if (orderParam is not null)
                assignments.Add($"{orderParam} = index");

            sb.AppendLine($"        var rows = list.Select((value, index) => new {{ {string.Join(", ", assignments)} }});");
        }

        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, Insert{ec.PropertyName}Sql, rows, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteMethod(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string idType,
        ElementCollectionModel ec)
    {
        sb.AppendLine($"    public async Task Delete{ec.PropertyName}Async({entityFqn} parent, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"Delete{ec.PropertyName}Async\";");
        sb.AppendLine($"        var parentId = parent.{entity.Properties.First(p => p.IsId).PropertyName};");
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, Delete{ec.PropertyName}Sql, new {{ parentId }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    internal static string BuildSelectSql(ElementCollectionModel ec)
    {
        var cols = string.Join(", ", ec.ValueColumns);
        if (ec.OrderColumnName is not null)
            cols += $", {ec.OrderColumnName}";
        return $"SELECT {cols} FROM {ec.CollectionTable} WHERE {ec.JoinColumn} = @parentId"
               + (ec.OrderColumnName is not null ? $" ORDER BY {ec.OrderColumnName}" : string.Empty);
    }

    internal static string BuildInsertSql(ElementCollectionModel ec)
    {
        var cols = new List<string> { ec.JoinColumn };
        cols.AddRange(ec.ValueColumns);
        if (ec.OrderColumnName is not null)
            cols.Add(ec.OrderColumnName);

        var parms = new List<string> { "@parentId" };
        foreach (var col in ec.ValueColumns)
            parms.Add($"@{SanitizeParam(col)}");
        if (ec.OrderColumnName is not null)
            parms.Add($"@{SanitizeParam(ec.OrderColumnName)}");

        return $"INSERT INTO {ec.CollectionTable} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", parms)})";
    }

    internal static string BuildDeleteSql(ElementCollectionModel ec)
        => $"DELETE FROM {ec.CollectionTable} WHERE {ec.JoinColumn} = @parentId";

    internal static string SanitizeParam(string column)
        => string.Concat(column.Split('_').Select((part, i) =>
            i == 0 ? part : char.ToUpperInvariant(part[0]) + part.Substring(1)));

    private static string Esc(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
