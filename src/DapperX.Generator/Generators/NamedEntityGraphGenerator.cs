namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Builders;
using DapperX.Generator.Emitters;
using DapperX.Generator.Models;

internal static class NamedEntityGraphGenerator
{
    private const string MainAlias = "e";

    public static IReadOnlyList<NamedEntityGraphModel> BuildGraphModels(
        EntityModel entity,
        IReadOnlyDictionary<string, EntityModel> allModels,
        string provider)
    {
        if (entity.NamedEntityGraphs.Count == 0)
            return [];

        return entity.NamedEntityGraphs
            .Select(g => new NamedEntityGraphModel
            {
                Name = g.Name,
                AttributeNodes = g.AttributeNodes,
                SubGraphs = g.SubGraphs,
                FromSql = BuildGraphFromSql(entity, g, allModels, provider),
                GeneratedSql = BuildGraphByIdSql(entity, g, allModels, provider),
            })
            .ToList();
    }

    public static void Emit(StringBuilder sb, EntityModel entity, string entityFqn, string idType, IReadOnlyDictionary<string, EntityModel> allModels, string provider)
    {
        var graphs = BuildGraphModels(entity, allModels, provider);
        if (graphs.Count == 0)
            return;

        foreach (var graph in graphs)
        {
            sb.AppendLine($"    private const string {graph.FromSqlConstantName} = \"{Esc(graph.FromSql)}\";");
            sb.AppendLine($"    private const string {graph.SqlConstantName} = \"{Esc(graph.GeneratedSql)}\";");
        }

        sb.AppendLine();
        sb.AppendLine($"    public async Task<{entityFqn}?> LoadGraphAsync({idType} id, string? entityGraph, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"LoadGraphAsync\";");
        sb.AppendLine("        var sql = ResolveNamedEntityGraphSql(entityGraph);");
        EntityQueryEmitter.EmitQueryFirstOrDefaultAsync(sb, entity, entityFqn, "sql", "new { Id = id }", "transaction", "__graphEntity");
        sb.AppendLine("        return __graphEntity;");
        sb.AppendLine("    }");
        sb.AppendLine();
        EmitGraphSqlSwitch(sb, graphs);
        EmitGraphFromSqlSwitch(sb, graphs);
    }

    private static void EmitGraphSqlSwitch(StringBuilder sb, IReadOnlyList<NamedEntityGraphModel> graphs)
    {
        sb.AppendLine("    private string ResolveNamedEntityGraphSql(string? entityGraph) => entityGraph switch");
        sb.AppendLine("    {");
        sb.AppendLine("        null => SelectByIdSql,");
        foreach (var graph in graphs)
            sb.AppendLine($"        \"{graph.Name}\" => {graph.SqlConstantName},");
        sb.AppendLine("        _ => throw new DapperX.Abstractions.Graphs.InvalidEntityGraphException(entityGraph ?? string.Empty),");
        sb.AppendLine("    };");
        sb.AppendLine();
    }

    private static void EmitGraphFromSqlSwitch(StringBuilder sb, IReadOnlyList<NamedEntityGraphModel> graphs)
    {
        sb.AppendLine("    private string ResolveNamedEntityGraphFromSql(string? entityGraph) => entityGraph switch");
        sb.AppendLine("    {");
        sb.AppendLine("        null => throw new DapperX.Abstractions.Graphs.InvalidEntityGraphException(string.Empty),");
        foreach (var graph in graphs)
            sb.AppendLine($"        \"{graph.Name}\" => {graph.FromSqlConstantName},");
        sb.AppendLine("        _ => throw new DapperX.Abstractions.Graphs.InvalidEntityGraphException(entityGraph ?? string.Empty),");
        sb.AppendLine("    };");
        sb.AppendLine();
    }

    internal static string BuildGraphByIdSql(
        EntityModel entity,
        NamedEntityGraphModel graph,
        IReadOnlyDictionary<string, EntityModel> allModels,
        string provider)
    {
        var fromSql = BuildGraphFromSql(entity, graph, allModels, provider);
        var idProp = entity.Properties.First(p => p.IsId);
        var sql = $"{fromSql} WHERE {MainAlias}.{idProp.ColumnName} = @id";
        return FilterInjector.AppendAllFilters(sql, entity, provider, MainAlias);
    }

    internal static string BuildGraphFromSql(
        EntityModel entity,
        NamedEntityGraphModel graph,
        IReadOnlyDictionary<string, EntityModel> allModels,
        string provider)
    {
        var cols = entity.Properties
            .Where(p => !p.IsTransient)
            .Select(p => $"{MainAlias}.{p.ColumnName}");
        var table = entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;
        var sql = $"SELECT {string.Join(", ", cols)} FROM {table} {MainAlias}";

        var joined = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in graph.AttributeNodes)
            sql = AppendJoinForNode(sql, entity, MainAlias, node, allModels, provider, joined);

        foreach (var subGraph in graph.SubGraphs)
            sql = AppendSubGraphJoins(sql, entity, MainAlias, subGraph, allModels, provider, joined);

        return sql;
    }

    private static string AppendSubGraphJoins(
        string sql,
        EntityModel parentEntity,
        string parentAlias,
        SubGraphModel subGraph,
        IReadOnlyDictionary<string, EntityModel> allModels,
        string provider,
        HashSet<string> joined)
    {
        var rel = parentEntity.Relationships.FirstOrDefault(r => r.PropertyName == subGraph.RelationshipProperty);
        if (rel is null)
            return sql;

        var (sqlAfterRel, childEntity, childAlias) = AppendRelationshipJoin(
            sql, parentEntity, parentAlias, rel, allModels, provider, joined);
        if (childEntity is null || childAlias is null)
            return sql;

        foreach (var node in subGraph.AttributeNodes)
            sqlAfterRel = AppendJoinForNode(sqlAfterRel, childEntity, childAlias, node, allModels, provider, joined);

        return sqlAfterRel;
    }

    private static string AppendJoinForNode(
        string sql,
        EntityModel entity,
        string parentAlias,
        string nodeName,
        IReadOnlyDictionary<string, EntityModel> allModels,
        string provider,
        HashSet<string> joined)
    {
        var rel = entity.Relationships.FirstOrDefault(r => r.PropertyName == nodeName);
        if (rel is null)
            return sql;

        var (result, _, _) = AppendRelationshipJoin(sql, entity, parentAlias, rel, allModels, provider, joined);
        return result;
    }

    private static (string Sql, EntityModel? ChildEntity, string? ChildAlias) AppendRelationshipJoin(
        string sql,
        EntityModel parentEntity,
        string parentAlias,
        RelationshipModel rel,
        IReadOnlyDictionary<string, EntityModel> allModels,
        string provider,
        HashSet<string> joined)
    {
        var joinKey = $"{parentAlias}:{rel.PropertyName}";
        if (!joined.Add(joinKey))
            return (sql, null, null);

        var target = ResolveTargetEntity(rel, allModels);
        if (target is null)
            return (sql, null, null);

        var joinAlias = $"g_{rel.PropertyName}";
        var targetTable = target.Schema is not null ? $"{target.Schema}.{target.TableName}" : target.TableName;
        var parentIdColumn = parentEntity.Properties.First(p => p.IsId).ColumnName;

        string onClause;
        if (rel.Kind == "OneToMany")
        {
            if (string.IsNullOrEmpty(rel.ForeignKeyColumn))
                return (sql, null, null);
            onClause = $"{joinAlias}.{rel.ForeignKeyColumn} = {parentAlias}.{parentIdColumn}";
        }
        else if (rel.Kind is "ManyToOne" or "OneToOne")
        {
            if (string.IsNullOrEmpty(rel.ForeignKeyColumn) || rel.IsPrimaryKeyJoin)
                return (sql, null, null);
            var targetId = target.Properties.FirstOrDefault(p => p.IsId)?.ColumnName ?? "id";
            onClause = $"{parentAlias}.{rel.ForeignKeyColumn} = {joinAlias}.{targetId}";
        }
        else
        {
            return (sql, null, null);
        }

        onClause = FilterInjector.AppendJoinFilters(onClause, target, joinAlias, provider);
        var joinSql = sql + $" INNER JOIN {targetTable} {joinAlias} ON {onClause}";
        return (joinSql, target, joinAlias);
    }

    private static EntityModel? ResolveTargetEntity(RelationshipModel rel, IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var fqn = rel.ChildEntityFqn ?? rel.TargetEntity;
        if (string.IsNullOrEmpty(fqn))
            return null;

        var key = fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring(8) : fqn;
        if (allModels.TryGetValue(key, out var model))
            return model;

        return allModels.Values.FirstOrDefault(m =>
            m.FullyQualifiedName == key || m.ClassName == key);
    }

    private static string Esc(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
}
