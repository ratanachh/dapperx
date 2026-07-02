using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Generators;

using System.Text;
using Generator.Models;

internal static class ElementCollectionLifecycleEmitter
{
    public static void EmitPersistLoadedOnInsert(StringBuilder sb, EntityModel entity, string indent = "        ")
    {
        foreach (var ec in entity.ElementCollections)
        {
            sb.AppendLine($"{indent}if (entity.{ec.PropertyName}.IsLoaded)");
            sb.AppendLine($"{indent}    await Insert{ec.PropertyName}Async(entity, entity.{ec.PropertyName}.TryGet() ?? Array.Empty<{ec.ElementTypeName}>(), transaction, ct);");
        }
    }

    public static void EmitReplaceLoadedOnUpdate(StringBuilder sb, EntityModel entity, string indent = "        ")
    {
        foreach (var ec in entity.ElementCollections)
        {
            sb.AppendLine($"{indent}if (entity.{ec.PropertyName}.IsLoaded)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    await Delete{ec.PropertyName}Async(entity, transaction, ct);");
            sb.AppendLine($"{indent}    await Insert{ec.PropertyName}Async(entity, entity.{ec.PropertyName}.TryGet() ?? Array.Empty<{ec.ElementTypeName}>(), transaction, ct);");
            sb.AppendLine($"{indent}}}");
        }
    }

    public static void EmitDeleteAllBeforeParentDelete(StringBuilder sb, EntityModel entity, string indent = "        ")
    {
        foreach (var ec in entity.ElementCollections)
            sb.AppendLine($"{indent}await Delete{ec.PropertyName}Async(entity, transaction, ct);");
    }

    public static void EmitDeleteAllByParentId(StringBuilder sb, EntityModel entity, string parentIdExpr, string indent = "        ")
    {
        foreach (var ec in entity.ElementCollections)
            sb.AppendLine($"{indent}await DbExecutor.ExecuteAsync(_connection, Delete{ec.PropertyName}Sql, new {{ parentId = {parentIdExpr} }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
    }
}
