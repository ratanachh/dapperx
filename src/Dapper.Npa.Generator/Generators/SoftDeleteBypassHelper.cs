using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Generators;

using System.Text;
using Generator.Models;

/// <summary>Compile-time paired SQL selection for <c>[SoftDelete]</c> read paths (Rule A).</summary>
internal static class SoftDeleteBypassHelper
{
    public const string IncludeDeletedParam = "bool includeDeleted = false";

    public static bool HasSoftDelete(EntityModel entity) => entity.SoftDeleteColumn is not null;

    /// <summary>Expression choosing active vs including-deleted SQL constant (e.g. SelectByIdSql).</summary>
    public static string BaseSqlExpression(EntityModel entity, string sqlPropertyPrefix)
    {
        if (!HasSoftDelete(entity))
            return $"{sqlPropertyPrefix}Sql";
        return $"includeDeleted ? {sqlPropertyPrefix}SqlIncludingDeleted : {sqlPropertyPrefix}Sql";
    }

    public static void EmitBaseSqlVariable(StringBuilder sb, EntityModel entity, string sqlPropertyPrefix, string variableName = "baseSql")
    {
        if (!HasSoftDelete(entity))
            sb.AppendLine($"        var {variableName} = {sqlPropertyPrefix}Sql;");
        else
            sb.AppendLine($"        var {variableName} = includeDeleted ? {sqlPropertyPrefix}SqlIncludingDeleted : {sqlPropertyPrefix}Sql;");
    }
}
