using DapperX.Generator.Models;

namespace DapperX.Generator.Utils;

using System.Linq;
using Generator.Models;

internal static class CompositeKeySqlHelper
{
    public static string BuildIdWhereClause(EntityModel entity)
    {
        var ck = entity.CompositeKey!;
        var parts = ck.Parts.Select(p =>
            $"{QualifyColumn(entity, p.ColumnName)} = @{p.KeyClassPropertyName}");
        return string.Join(" AND ", parts);
    }

    public static string BuildEntityIdWhereClause(EntityModel entity)
    {
        var ck = entity.CompositeKey!;
        if (!ck.IsEmbeddedId)
            return BuildIdWhereClause(entity);

        var parts = ck.Parts.Select(p =>
            $"{QualifyColumn(entity, p.ColumnName)} = @{p.KeyClassPropertyName}");
        return string.Join(" AND ", parts);
    }

    public static string BuildIdParamObject(CompositeKeyModel compositeKey, string idVariable)
        => "new { " + string.Join(", ",
            compositeKey.Parts.Select(p => $"{p.KeyClassPropertyName} = {IdAccess(idVariable, p)}")) + " }";

    public static string BuildEntityIdParamObject(CompositeKeyModel compositeKey, string entityVariable)
    {
        if (!compositeKey.IsEmbeddedId)
            return entityVariable;

        return "new { " + string.Join(", ",
            compositeKey.Parts.Select(p =>
                $"{p.KeyClassPropertyName} = {entityVariable}.{compositeKey.EmbeddedIdPropertyName}.{p.EmbeddedInnerProperty}")) + " }";
    }

    public static string IdAccess(string idVariable, CompositeKeyPartModel part)
        => $"{idVariable}.{part.KeyClassPropertyName}";

    public static string ResolveIdTypeName(EntityModel entity, RepositoryInterfaceModel? repositoryInterface)
    {
        if (!string.IsNullOrEmpty(repositoryInterface?.IdTypeName))
            return repositoryInterface!.IdTypeName;

        if (entity.CompositeKey is not null)
            return FormatClrType(entity.CompositeKey.KeyTypeName);

        var idProp = entity.Properties.FirstOrDefault(p => p.IsId);
        return idProp is null ? "object" : FormatClrType(idProp.ClrTypeName);
    }

    public static string FormatClrType(string clrTypeName)
    {
        return clrTypeName switch
        {
            "int" => "int",
            "long" => "long",
            "string" => "string",
            "Guid" => "global::System.Guid",
            "short" => "short",
            "byte" => "byte",
            "decimal" => "decimal",
            "double" => "double",
            "float" => "float",
            var t when t.StartsWith("global::", StringComparison.Ordinal) => t,
            var t when t.Contains('.') => $"global::{t}",
            var t => t,
        };
    }

    private static string QualifyColumn(EntityModel entity, string columnName)
        => entity.SecondaryTables.Any() ? $"e.{columnName}" : columnName;
}
