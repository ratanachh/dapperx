using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Builders;

using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class StoredProcedureMetadataBuilder
{
    public static StoredProcedureModel Build(AttributeData attr, IMethodSymbol method)
    {
        var procName = SyntaxHelper.GetConstructorArg<string>(attr, 0) ?? string.Empty;
        var outNames = SyntaxHelper.GetStringArrayNamedArg(attr, "OutParameters");
        var inOutNames = SyntaxHelper.GetStringArrayNamedArg(attr, "InOutParameters");
        var returnParameter = SyntaxHelper.GetNamedArg<string>(attr, "ReturnParameter");
        var resultSets = SyntaxHelper.GetTypeArrayNamedArg(attr, "ResultSets");
        var returnShape = AnalyzeReturnShape(method, out var procResultTypeArgs);

        var inOutSet = new HashSet<string>(inOutNames, StringComparer.OrdinalIgnoreCase);
        var outSet = new HashSet<string>(outNames, StringComparer.OrdinalIgnoreCase);
        var parameters = new List<ProcParamModel>();

        foreach (var p in GetMethodParameters(method))
        {
            var mode = "In";
            if (inOutSet.Contains(p.Name))
                mode = "InOut";
            else if (outSet.Contains(p.Name))
                mode = "Out";

            parameters.Add(new ProcParamModel
            {
                Name = p.Name,
                Mode = mode,
                ClrTypeName = p.Type.ToDisplayString(),
            });
        }

        foreach (var outName in outNames)
        {
            if (parameters.Any(p => p.Name.Equals(outName, StringComparison.OrdinalIgnoreCase)))
                continue;

            parameters.Add(new ProcParamModel
            {
                Name = outName,
                Mode = "Out",
                ClrTypeName = ResolveOutClrType(outName, outNames, procResultTypeArgs),
            });
        }

        if (!string.IsNullOrEmpty(returnParameter))
        {
            parameters.Add(new ProcParamModel
            {
                Name = returnParameter!,
                Mode = "Return",
                ClrTypeName = procResultTypeArgs.FirstOrDefault(),
            });
        }

        return new StoredProcedureModel
        {
            ProcName = procName,
            Parameters = parameters,
            ResultSetTypes = resultSets,
            OutParameterNames = outNames,
            ReturnParameterName = returnParameter,
            ReturnKind = returnShape,
            ProcResultTypeArguments = procResultTypeArgs,
        };
    }

    private static IEnumerable<IParameterSymbol> GetMethodParameters(IMethodSymbol method)
        => method.Parameters.Where(p => p.Name is not "transaction" and not "ct");

    private static string? ResolveOutClrType(
        string outName,
        IReadOnlyList<string> outNames,
        IReadOnlyList<string> procResultTypeArgs)
    {
        var index = outNames
            .Select((name, i) => (name, i))
            .FirstOrDefault(x => x.name.Equals(outName, StringComparison.OrdinalIgnoreCase))
            .i;
        return index >= 0 && index < procResultTypeArgs.Count
            ? procResultTypeArgs[index]
            : null;
    }

    private static StoredProcedureReturnKind AnalyzeReturnShape(
        IMethodSymbol method,
        out IReadOnlyList<string> procResultTypeArgs)
    {
        procResultTypeArgs = [];
        if (method.ReturnType is not INamedTypeSymbol taskType
            || !taskType.IsGenericType
            || taskType.Name != "Task")
            return StoredProcedureReturnKind.Invalid;

        var inner = taskType.TypeArguments[0];
        if (inner.SpecialType == SpecialType.System_Void)
            return StoredProcedureReturnKind.Void;

        if (inner is not INamedTypeSymbol named)
            return StoredProcedureReturnKind.Invalid;

        var ns = named.ContainingNamespace?.ToDisplayString();
        if (named.Name == "ProcResult"
            && ns is "DapperX.Abstractions.StoredProcedures" or "global::DapperX.Abstractions.StoredProcedures")
        {
            procResultTypeArgs = named.TypeArguments.Select(t => t.ToDisplayString()).ToList();
            return procResultTypeArgs.Count switch
            {
                1 => StoredProcedureReturnKind.ProcResult1,
                2 => StoredProcedureReturnKind.ProcResult2,
                _ => StoredProcedureReturnKind.Invalid,
            };
        }

        if (named.Name == "MultiResult"
            && ns is "DapperX.Abstractions.StoredProcedures" or "global::DapperX.Abstractions.StoredProcedures")
            return named.TypeArguments.Length == 2
                ? StoredProcedureReturnKind.MultiResult2
                : StoredProcedureReturnKind.Invalid;

        if (named.IsGenericType && named.Name == "IEnumerable")
            return StoredProcedureReturnKind.EntityEnumerable;

        return StoredProcedureReturnKind.Invalid;
    }
}
