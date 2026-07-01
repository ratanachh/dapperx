namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Builders;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class QueryGenerator
{
    private const string MainAlias = "e";

    public static void Emit(
        StringBuilder sb,
        EntityModel entity,
        string provider,
        IReadOnlyDictionary<string, EntityModel> allModels,
        IReadOnlyList<ProjectionModel> projections,
        SourceProductionContext spc)
    {
        // SQLite pessimistic IQuery.WithLock: runtime throw in QueryLockSuffix (no blanket compile error per entity).
        // Derived LockMode parameter on SQLite: DPX037 at method level.

        if (provider == "MySql")
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MySqlLockTimeoutUnsupported,
                Location.None,
                entity.ClassName));
        }

        var baseSql = SqlBuilder.BuildQueryBaseSelect(entity, MainAlias);
        var countFrom = SqlBuilder.BuildQueryCountFrom(entity, MainAlias);
        var eagerJoinSql = BuildEagerJoinSql(entity, allModels);
        if (!string.IsNullOrEmpty(eagerJoinSql))
        {
            baseSql += eagerJoinSql;
            countFrom += eagerJoinSql;
        }

        sb.AppendLine($"    private const string QueryBaseSql = \"{Esc(baseSql)}\";");
        sb.AppendLine($"    private const string QueryCountFromClause = \"{Esc(countFrom)}\";");
        sb.AppendLine();

        EmitProjectionCatalog(sb, projections);

        EmitIncludeJoinSwitch(sb, entity, allModels);
        EmitSplitIncludeMethods(sb, entity, allModels, provider);

        sb.AppendLine($"    public override IQuery<{entity.FullyQualifiedName}> Query()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var config = new DapperX.Runtime.Query.QueryRuntimeConfig");
        sb.AppendLine("        {");
        sb.AppendLine($"            Provider = \"{provider}\",");
        sb.AppendLine($"            MainAlias = \"{MainAlias}\",");
        sb.AppendLine($"            SoftDeleteSupported = {(entity.SoftDeleteColumn is not null ? "true" : "false")},");
        if (entity.SoftDeleteColumn is not null)
            sb.AppendLine($"            SoftDeleteColumn = \"{entity.SoftDeleteColumn}\",");
        if (entity.TenantIdColumn is not null)
            sb.AppendLine($"            TenantIdColumn = \"{entity.TenantIdColumn}\",");
        if (entity.GlobalFilters.Any())
        {
            sb.AppendLine("            ApplyGlobalFilters = sql => ApplyGlobalFilters(sql),");
            var filterNames = string.Join(", ", entity.GlobalFilters.Select(gf => $"\"{gf.Name}\""));
            sb.AppendLine($"            GlobalFilterNames = new[] {{ {filterNames} }},");
        }
        if (entity.TenantIdColumn is not null)
            sb.AppendLine("            GetTenantId = () => _tenantProvider?.GetCurrentTenantId(),");
        sb.AppendLine("            IncludeJoinSql = QueryIncludeJoinSql,");
        if (projections.Count > 0)
            sb.AppendLine("            ProjectionBaseSql = QueryProjectionBaseSql,");
        sb.AppendLine("        };");
        sb.AppendLine($"        return new DapperX.Runtime.Query.RepositoryQuery<{entity.FullyQualifiedName}>(");
        sb.AppendLine("            _connection,");
        sb.AppendLine("            QueryBaseSql,");
        sb.AppendLine("            QueryCountFromClause,");
        sb.AppendLine($"            {entity.ClassName}RepositoryImpl.ResolveColumn,");
        sb.AppendLine("            config,");

        if (entity.HasPostLoad)
            sb.AppendLine("            postLoad: OnPostLoad,");
        else
            sb.AppendLine("            postLoad: null,");

        var hasSplitIncludes = entity.Relationships.Any(r =>
            r.Kind is "ManyToOne" or "OneToOne"
            && !string.IsNullOrEmpty(r.ForeignKeyColumn)
            && !r.IsPrimaryKeyJoin);
        if (hasSplitIncludes)
            sb.AppendLine("            applySplitIncludes: ApplyQuerySplitIncludesAsync,");
        else
            sb.AppendLine("            applySplitIncludes: null,");

        sb.AppendLine("            options: Options,");
        sb.AppendLine($"            provider: Provider);");

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitProjectionCatalog(StringBuilder sb, IReadOnlyList<ProjectionModel> projections)
    {
        if (projections.Count == 0)
            return;

        sb.AppendLine("    private static readonly IReadOnlyDictionary<string, string> QueryProjectionBaseSql =");
        sb.AppendLine("        new Dictionary<string, string>(StringComparer.Ordinal)");
        sb.AppendLine("        {");
        foreach (var p in projections)
        {
            sb.AppendLine($"            [\"{Esc(p.DtoDisplayName)}\"] = \"{Esc(p.BaseSelectSql)}\",");
            sb.AppendLine($"            [\"{Esc(p.DtoMetadataName)}\"] = \"{Esc(p.BaseSelectSql)}\",");
        }
        sb.AppendLine("        };");
        sb.AppendLine();
    }

    private static void EmitIncludeJoinSwitch(
        StringBuilder sb,
        EntityModel entity,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var joins = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rel in entity.Relationships.Where(r => r.Kind is "ManyToOne" or "OneToOne"))
        {
            if (string.IsNullOrEmpty(rel.ForeignKeyColumn) && !rel.IsPrimaryKeyJoin)
                continue;
            var target = ResolveTargetEntity(rel, allModels);
            if (target is null) continue;
            joins[rel.PropertyName] = RelationshipSqlBuilder.BuildReferenceJoinSql(entity, MainAlias, rel, target);
        }

        sb.AppendLine("    private static readonly IReadOnlyDictionary<string, string> QueryIncludeJoinSql =");
        sb.AppendLine("        new Dictionary<string, string>(StringComparer.Ordinal)");
        sb.AppendLine("        {");
        foreach (var kv in joins)
            sb.AppendLine($"            [\"{kv.Key}\"] = \"{Esc(kv.Value)}\",");
        sb.AppendLine("        };");
        sb.AppendLine();
    }

    private static void EmitSplitIncludeMethods(
        StringBuilder sb,
        EntityModel entity,
        IReadOnlyDictionary<string, EntityModel> allModels,
        string provider)
    {
        var navigations = entity.Relationships
            .Where(r => r.Kind is "ManyToOne" or "OneToOne"
                        && !string.IsNullOrEmpty(r.ForeignKeyColumn)
                        && !r.IsPrimaryKeyJoin)
            .ToList();
        if (navigations.Count == 0)
            return;

        sb.AppendLine($"    private async Task ApplyQuerySplitIncludesAsync(");
        sb.AppendLine($"        IList<{entity.FullyQualifiedName}> entities,");
        sb.AppendLine("        IReadOnlyList<string> includes,");
        sb.AppendLine("        IDbTransaction? transaction)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (entities.Count == 0) return;");
        foreach (var rel in navigations)
        {
            var target = ResolveTargetEntity(rel, allModels);
            if (target is null) continue;
            sb.AppendLine($"        if (includes.Contains(\"{rel.PropertyName}\"))");
            sb.AppendLine($"            await Attach{rel.PropertyName}ForQueryAsync(entities, transaction);");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        var idProp = entity.Properties.First(p => p.IsId);
        foreach (var rel in navigations)
        {
            var target = ResolveTargetEntity(rel, allModels);
            if (target is null) continue;

            var fkProp = entity.Properties.FirstOrDefault(p =>
                string.Equals(p.ColumnName, rel.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.PropertyName, rel.PropertyName + "Id", StringComparison.OrdinalIgnoreCase));

            var targetFqn = target.FullyQualifiedName;
            var targetIdProp = target.Properties.First(p => p.IsId);
            var select = SqlBuilder.BuildSelect(target, provider: provider);
            var fkColumn = rel.ForeignKeyColumn;

            sb.AppendLine($"    private async Task Attach{rel.PropertyName}ForQueryAsync(");
            sb.AppendLine($"        IList<{entity.FullyQualifiedName}> entities, IDbTransaction? transaction)");
            sb.AppendLine("    {");
            sb.AppendLine($"        const string MethodName = \"Query.Attach{rel.PropertyName}ForQueryAsync\";");

            if (fkProp is not null)
            {
                sb.AppendLine($"        var ids = entities.Select(e => e.{fkProp.PropertyName}).Distinct().ToList();");
                sb.AppendLine("        if (ids.Count == 0) return;");
                var attachByFkSql = AppendInClauseFilter(select, ProviderSqlHelper.InClause(targetIdProp.ColumnName, "ids", provider));
                sb.AppendLine($"        var related = (await DbExecutor.QueryAsync<{targetFqn}>(_connection,");
                sb.AppendLine($"            \"{Esc(attachByFkSql)}\",");
                sb.AppendLine("            new { ids }, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider)).ConfigureAwait(false)).ToDictionary(r => r." +
                              $"{targetIdProp.PropertyName});");
                sb.AppendLine("        foreach (var e in entities)");
                sb.AppendLine($"            if (related.TryGetValue(e.{fkProp.PropertyName}, out var nav))");
                sb.AppendLine($"                e.{rel.PropertyName} = nav;");
            }
            else
            {
                var table = entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;
                sb.AppendLine($"        var parentIds = entities.Select(e => e.{idProp.PropertyName}).Distinct().ToList();");
                sb.AppendLine("        if (parentIds.Count == 0) return;");
                sb.AppendLine($"        var links = (await DbExecutor.QueryAsync<({idProp.ClrTypeName} ParentId, {targetIdProp.ClrTypeName} Fk)>(_connection,");
                sb.AppendLine($"            \"SELECT {idProp.ColumnName}, {fkColumn} FROM {table} WHERE {ProviderSqlHelper.InClause(idProp.ColumnName, "parentIds", provider)}\",");
                sb.AppendLine("            new { parentIds }, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider)).ConfigureAwait(false)).ToDictionary(x => x.ParentId, x => x.Fk);");
                sb.AppendLine("        var fkIds = links.Values.Distinct().ToList();");
                sb.AppendLine("        if (fkIds.Count == 0) return;");
                var attachByFkIdsSql = AppendInClauseFilter(select, ProviderSqlHelper.InClause(targetIdProp.ColumnName, "fkIds", provider));
                sb.AppendLine($"        var related = (await DbExecutor.QueryAsync<{targetFqn}>(_connection,");
                sb.AppendLine($"            \"{Esc(attachByFkIdsSql)}\",");
                sb.AppendLine("            new { fkIds }, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider)).ConfigureAwait(false)).ToDictionary(r => r." +
                              $"{targetIdProp.PropertyName});");
                sb.AppendLine($"        foreach (var e in entities)");
                sb.AppendLine($"            if (links.TryGetValue(e.{idProp.PropertyName}, out var fk) && related.TryGetValue(fk, out var nav))");
                sb.AppendLine($"                e.{rel.PropertyName} = nav;");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    private static EntityModel? ResolveTargetEntity(RelationshipModel rel, IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var fqn = rel.TargetEntity;
        if (fqn is null || fqn.Length == 0) return null;
        var key = fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring(8) : fqn;
        if (allModels.TryGetValue(key, out var model)) return model;
        foreach (var m in allModels.Values)
        {
            if (string.Equals(m.FullyQualifiedName, key, StringComparison.Ordinal))
                return m;
        }
        return null;
    }

    private static string BuildEagerJoinSql(EntityModel entity, IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var joins = new List<string>();
        foreach (var rel in entity.Relationships.Where(r => r.Fetch == "Eager" && r.Kind is "ManyToOne" or "OneToOne"))
        {
            if (string.IsNullOrEmpty(rel.ForeignKeyColumn) && !rel.IsPrimaryKeyJoin)
                continue;

            var target = ResolveTargetEntity(rel, allModels);
            if (target is null)
                continue;

            joins.Add(RelationshipSqlBuilder.BuildReferenceJoinSql(entity, MainAlias, rel, target));
        }

        return string.Concat(joins);
    }

    private static string AppendInClauseFilter(string selectSql, string inClause)
        => selectSql.Contains(" WHERE ", StringComparison.Ordinal)
            ? $"{selectSql} AND {inClause}"
            : $"{selectSql} WHERE {inClause}";

    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
