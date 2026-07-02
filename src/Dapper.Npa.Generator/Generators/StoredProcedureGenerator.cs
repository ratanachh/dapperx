using Dapper.Npa.Generator.Builders;
using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Generators;

using System.Text;
using Generator.Builders;
using Generator.Models;
using Microsoft.CodeAnalysis;

internal static class StoredProcedureGenerator
{
    public static string EmitMethod(
        IMethodSymbol method,
        EntityModel entity,
        AttributeData attr,
        string provider)
    {
        var model = StoredProcedureMetadataBuilder.Build(attr, method);
        var entityFqn = entity.FullyQualifiedName;
        var sig = BuildSignature(method);

        if (model.HasMultipleResultSets)
            return EmitMultiResultMethod(method, model, sig, provider);

        if (model.HasOutputParameters)
            return EmitProcResultMethod(method, model, sig, provider);

        return EmitEntityQueryMethod(method, entity, model, entityFqn, sig, provider);
    }

    private static string EmitEntityQueryMethod(
        IMethodSymbol method,
        EntityModel entity,
        StoredProcedureModel model,
        string entityFqn,
        string sig,
        string provider)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    {sig}");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string sql = \"{Esc(BuildCallSql(model, provider))}\";");
        sb.AppendLine("        var dp = new DynamicParameters();");
        EmitInputParameters(sb, model);
        sb.AppendLine($"        var __rows = (await DbExecutor.QueryAsync<{entityFqn}>(_connection, sql, dp, {TransactionExpression(method)}, commandType: {CommandTypeExpression(provider, model)}, logContext: DbExecutor.CreateLogContext(\"{method.Name}\", Options, Provider))).AsList();");
        EmitPostLoadMany(sb, entity, "__rows");
        sb.AppendLine("        return __rows;");
        sb.AppendLine("    }");
        return sb.ToString();
    }

    private static string EmitProcResultMethod(
        IMethodSymbol method,
        StoredProcedureModel model,
        string sig,
        string provider)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    {sig}");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string sql = \"{Esc(BuildCallSql(model, provider))}\";");
        sb.AppendLine("        var dp = new DynamicParameters();");
        EmitAllParameters(sb, model, method);
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, sql, dp, {TransactionExpression(method)}, commandType: {CommandTypeExpression(provider, model)}, logContext: DbExecutor.CreateLogContext(\"{method.Name}\", Options, Provider));");

        var outCaptureVars = new List<string>();
        foreach (var param in model.Parameters.Where(p => p.Mode is not "In"))
        {
            var varName = SanitizeVarName(param.Name);
            var clrType = param.ClrTypeName ?? "object";
            sb.AppendLine($"        var {varName} = dp.Get<{clrType}>(\"@{param.Name}\");");
            outCaptureVars.Add(param.Name);
        }

        sb.AppendLine("        var __outputs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("        {");
        foreach (var name in outCaptureVars)
            sb.AppendLine($"            [\"{name}\"] = {SanitizeVarName(name)},");
        sb.AppendLine("        };");

        if (model.ReturnKind == StoredProcedureReturnKind.ProcResult1)
        {
            var primary = model.OutParameterNames.FirstOrDefault()
                ?? model.ReturnParameterName
                ?? model.Parameters.First(p => p.Mode is not "In").Name;
            sb.AppendLine($"        return new Dapper.Npa.Abstractions.StoredProcedures.ProcResult<{model.ProcResultTypeArguments[0]}>({SanitizeVarName(primary)}, __outputs);");
        }
        else
        {
            sb.AppendLine($"        return new Dapper.Npa.Abstractions.StoredProcedures.ProcResult<{model.ProcResultTypeArguments[0]}, {model.ProcResultTypeArguments[1]}>({SanitizeVarName(model.OutParameterNames[0])}, {SanitizeVarName(model.OutParameterNames[1])}, __outputs);");
        }

        sb.AppendLine("    }");
        return sb.ToString();
    }

    private static string EmitMultiResultMethod(
        IMethodSymbol method,
        StoredProcedureModel model,
        string sig,
        string provider)
    {
        var t1 = model.ResultSetTypes[0];
        var t2 = model.ResultSetTypes[1];
        var sb = new StringBuilder();
        sb.AppendLine($"    {sig}");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string sql = \"{Esc(BuildCallSql(model, provider))}\";");
        sb.AppendLine("        var dp = new DynamicParameters();");
        EmitInputParameters(sb, model);
        sb.AppendLine($"        using var __grid = await DbExecutor.QueryMultipleAsync(_connection, sql, dp, {TransactionExpression(method)}, commandType: {CommandTypeExpression(provider, model)}, logContext: DbExecutor.CreateLogContext(\"{method.Name}\", Options, Provider));");
        sb.AppendLine($"        var __first = (await __grid.ReadAsync<{t1}>()).AsList();");
        sb.AppendLine($"        var __second = (await __grid.ReadAsync<{t2}>()).AsList();");
        sb.AppendLine($"        return new Dapper.Npa.Abstractions.StoredProcedures.MultiResult<{t1}, {t2}>(__first, __second);");
        sb.AppendLine("    }");
        return sb.ToString();
    }

    private static void EmitInputParameters(StringBuilder sb, StoredProcedureModel model)
    {
        foreach (var param in model.Parameters.Where(p => p.Mode is "In"))
            sb.AppendLine($"        dp.Add(\"@{param.Name}\", {param.Name});");
    }

    private static void EmitAllParameters(StringBuilder sb, StoredProcedureModel model, IMethodSymbol method)
    {
        var methodParams = method.Parameters
            .Where(p => p.Name is not "transaction" and not "ct")
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var param in model.Parameters)
        {
            var direction = param.Mode switch
            {
                "Out" => "System.Data.ParameterDirection.Output",
                "InOut" => "System.Data.ParameterDirection.InputOutput",
                "Return" => "System.Data.ParameterDirection.ReturnValue",
                _ => "System.Data.ParameterDirection.Input",
            };

            if (param.Mode is "In" or "InOut" && methodParams.ContainsKey(param.Name))
            {
                sb.AppendLine($"        dp.Add(\"@{param.Name}\", {param.Name}, direction: {direction});");
                continue;
            }

            if (param.Mode is "Out" or "Return")
            {
                var dbType = DbTypeExpression(param.ClrTypeName);
                if (IsStringType(param.ClrTypeName))
                {
                    sb.AppendLine(
                        $"        dp.Add(\"@{param.Name}\", dbType: {dbType}, direction: {direction}, size: 4000);");
                }
                else
                {
                    sb.AppendLine(
                        $"        dp.Add(\"@{param.Name}\", dbType: {dbType}, direction: {direction});");
                }
            }
        }
    }

    private static bool IsStringType(string? clrTypeName)
        => clrTypeName is "string" or "System.String" or "string?" or "System.String?";

    private static string DbTypeExpression(string? clrTypeName)
        => clrTypeName switch
        {
            "int" or "System.Int32" or "int?" or "System.Int32?" => "System.Data.DbType.Int32",
            "long" or "System.Int64" or "long?" or "System.Int64?" => "System.Data.DbType.Int64",
            "decimal" or "System.Decimal" or "decimal?" or "System.Decimal?" => "System.Data.DbType.Decimal",
            "double" or "System.Double" or "double?" or "System.Double?" => "System.Data.DbType.Double",
            "bool" or "System.Boolean" or "bool?" or "System.Boolean?" => "System.Data.DbType.Boolean",
            "string" or "System.String" or "string?" or "System.String?" => "System.Data.DbType.String",
            "System.Guid" or "System.Guid?" => "System.Data.DbType.Guid",
            _ => "System.Data.DbType.Object",
        };

    private static void EmitPostLoadMany(StringBuilder sb, EntityModel entity, string variableName)
    {
        if (!entity.HasPostLoad)
            return;
        sb.AppendLine($"        foreach (var __e in {variableName}) OnPostLoad(__e);");
    }

    private static string BuildCallSql(StoredProcedureModel model, string provider)
    {
        var inArgs = string.Join(", ", model.Parameters.Where(p => p.Mode is "In").Select(p => $"@{p.Name}"));
        var inAndInOutArgs = string.Join(", ", model.Parameters.Where(p => p.Mode is "In" or "InOut").Select(p => $"@{p.Name}"));
        return provider switch
        {
            "SqlServer" => model.ProcName,
            "PostgreSql" when model.HasOutputParameters && !model.HasMultipleResultSets
                => $"CALL {model.ProcName}({BuildPostgreSqlCallArgs(model)})",
            "PostgreSql" => $"SELECT * FROM {model.ProcName}({inArgs})",
            "MySql" when model.HasOutputParameters && !model.HasMultipleResultSets
                => model.ProcName,
            "MySql" => $"CALL {model.ProcName}({inArgs})",
            _ => model.ProcName,
        };
    }

    /// <summary>PostgreSQL CALL requires every procedure argument; OUT-only slots use NULL (Npgsql binds outputs separately).</summary>
    private static string BuildPostgreSqlCallArgs(StoredProcedureModel model)
        => string.Join(", ", model.Parameters.Select(p => p.Mode is "Out" or "Return" ? "NULL" : $"@{p.Name}"));

    private static string CommandTypeExpression(string provider, StoredProcedureModel model)
        => provider switch
        {
            "SqlServer" => "System.Data.CommandType.StoredProcedure",
            "MySql" when model.HasOutputParameters && !model.HasMultipleResultSets
                => "System.Data.CommandType.StoredProcedure",
            _ => "null",
        };

    private static string BuildSignature(IMethodSymbol method)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        return $"public async {returnType} {method.Name}({parameters})";
    }

    private static string TransactionExpression(IMethodSymbol method)
        => method.Parameters.Any(p => p.Name == "transaction") ? "transaction" : "null";

    private static string SanitizeVarName(string name)
        => "__" + char.ToLowerInvariant(name[0]) + name.Substring(1);

    private static string Esc(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
