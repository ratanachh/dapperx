namespace DapperX.Generator.Generators;

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using DapperX.Generator.Emitters;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

internal static class CompositeKeyGenerator
{
    public static void ValidateEntity(EntityModel entity, Location? location, SourceProductionContext ctx)
    {
        if (!entity.HasCompositeKey || location is null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.CompositeKeyUpsertNotSupported,
            location,
            entity.ClassName));
    }

    private static readonly string[] BulkIdMethodNames =
    [
        "FindAllByIdAsync",
        "DeleteAllByIdAsync",
    ];

    public static void ValidateRepositoryInterface(
        EntityModel entity,
        RepositoryInterfaceModel? repositoryInterface,
        SourceProductionContext ctx)
    {
        if (!entity.HasCompositeKey || repositoryInterface is null)
            return;

        foreach (var method in repositoryInterface.DeclaredMethods)
        {
            if (!BulkIdMethodNames.Contains(method.Name))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.CompositeKeyBulkIdMethod,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName));
        }
    }

    public static void EmitOverrides(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string idType)
    {
        if (!entity.HasCompositeKey || entity.CompositeKey is null)
            return;

        var ck = entity.CompositeKey;
        var idParams = CompositeKeySqlHelper.BuildIdParamObject(ck, "id");

        EmitGetById(sb, entity, entityFqn, idType, idParams);
        EmitExistsById(sb, idType, idParams);
        EmitDeleteById(sb, idType, idParams);

        if (ck.IsEmbeddedId)
        {
            var entityIdParams = CompositeKeySqlHelper.BuildEntityIdParamObject(ck, "entity");
            EmitUpdateAsync(sb, entity, entityFqn, entityIdParams);
            EmitDeleteAsync(sb, entity, entityFqn, entityIdParams);
        }
    }

    private static void EmitGetById(StringBuilder sb, EntityModel entity, string entityFqn, string idType, string idParams)
    {
        sb.AppendLine($"    public override async Task<{entityFqn}?> GetByIdAsync({idType} id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetByIdAsync\";");
        EntityQueryEmitter.EmitQueryFirstOrDefaultAsync(sb, entity, entityFqn, "SelectByIdSql", idParams, "transaction", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(result);");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitExistsById(StringBuilder sb, string idType, string idParams)
    {
        sb.AppendLine($"    public override async Task<bool> ExistsByIdAsync({idType} id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"ExistsByIdAsync\";");
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<int>(_connection, ExistsSql, {idParams}, transaction{DbExecutorEmission.LogContextSuffix}) == 1;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteById(StringBuilder sb, string idType, string idParams)
    {
        sb.AppendLine($"    public override async Task DeleteByIdAsync({idType} id, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteByIdAsync\";");
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, DeleteByIdSql, {idParams}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitUpdateAsync(StringBuilder sb, EntityModel entity, string entityFqn, string entityIdParams)
    {
        sb.AppendLine($"    public override async Task UpdateAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"UpdateAsync\";");
        sb.AppendLine("        OnPreUpdate(entity);");
        sb.AppendLine($"        var affected = await DbExecutor.ExecuteAsync(_connection, UpdateSql, {entityIdParams}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        if (affected == 0) throw new DapperX.Abstractions.Exceptions.ConcurrencyException(\"Update failed — record may have been modified.\");");
        sb.AppendLine("        OnPostUpdate(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteAsync(StringBuilder sb, EntityModel entity, string entityFqn, string entityIdParams)
    {
        sb.AppendLine($"    public override async Task DeleteAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteAsync\";");
        sb.AppendLine("        OnPreRemove(entity);");
        sb.AppendLine($"        var affected = await DbExecutor.ExecuteAsync(_connection, DeleteSql, {entityIdParams}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        if (affected == 0) throw new DapperX.Abstractions.Exceptions.ConcurrencyException(\"Delete failed — record may have been modified.\");");
        sb.AppendLine("        OnPostRemove(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
