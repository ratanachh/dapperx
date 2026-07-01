namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Models;

internal static class AuditingGenerator
{
    public static void EmitPopulateBeforePersist(StringBuilder sb, EntityModel entity, bool isUpdate)
    {
        var auditing = entity.Auditing;
        if (auditing is null)
            return;

        if (!isUpdate && auditing.CreatedByProperty is not null)
            sb.AppendLine($"        if (_auditingProvider is not null) entity.{auditing.CreatedByProperty} = _auditingProvider.GetCurrentUser();");

        if (auditing.LastModifiedByProperty is not null)
            sb.AppendLine($"        if (_auditingProvider is not null) entity.{auditing.LastModifiedByProperty} = _auditingProvider.GetCurrentUser();");
    }

    public static void EmitMutatingOverridesIfNeeded(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        if (entity.Auditing is null || entity.SecondaryTables.Any())
            return;
        if (entity.Properties.Any(p => p.GeneratedTime is not null))
            return;

        EmitInsertOverride(sb, entity, entityFqn);
        EmitUpdateOverride(sb, entity, entityFqn);
    }

    private static void EmitInsertOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task InsertAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"InsertAsync\";");
        PrimaryKeyJoinColumnGenerator.EmitAssignBeforeInsert(sb, entity);
        EmitPopulateBeforePersist(sb, entity, isUpdate: false);
        sb.AppendLine("        OnPrePersist(entity);");
        sb.AppendLine("        await DbExecutor.ExecuteAsync(_connection, InsertSql, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        OnPostPersist(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitUpdateOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task UpdateAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"UpdateAsync\";");
        EmitPopulateBeforePersist(sb, entity, isUpdate: true);
        sb.AppendLine("        OnPreUpdate(entity);");
        sb.AppendLine($"        var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "UpdateSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        if (affected == 0) throw new DapperX.Abstractions.Exceptions.ConcurrencyException(\"Update failed — record may have been modified.\");");
        sb.AppendLine("        OnPostUpdate(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
