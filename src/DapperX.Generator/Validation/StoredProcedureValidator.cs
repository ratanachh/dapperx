using DapperX.Generator.Builders;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Generator.Builders;
using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class StoredProcedureValidator
{
    public static bool Validate(
        IMethodSymbol method,
        EntityModel entity,
        AttributeData attr,
        string provider,
        SourceProductionContext ctx)
    {
        var model = StoredProcedureMetadataBuilder.Build(attr, method);
        var location = method.Locations.FirstOrDefault();
        var valid = true;

        if (string.IsNullOrEmpty(model.ProcName))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.StoredProcedureReturnTypeMismatch,
                location,
                method.Name,
                entity.ClassName));
            valid = false;
        }

        var methodParamNames = new HashSet<string>(
            method.Parameters
                .Where(p => p.Name is not "transaction" and not "ct")
                .Select(p => p.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var inOut in SyntaxHelper.GetStringArrayNamedArg(attr, "InOutParameters"))
        {
            if (methodParamNames.Contains(inOut))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.StoredProcedureUnknownParameter,
                location,
                model.ProcName,
                method.Name,
                inOut));
            valid = false;
        }

        foreach (var outParam in model.OutParameterNames)
        {
            if (methodParamNames.Contains(outParam))
                continue;
            if (model.ProcResultTypeArguments.Count > 0)
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.StoredProcedureUnknownParameter,
                location,
                model.ProcName,
                method.Name,
                outParam));
            valid = false;
        }

        if (model.HasMultipleResultSets)
        {
            if (model.ResultSetTypes.Count != 2)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.StoredProcedureResultSetCountMismatch,
                    location,
                    method.Name,
                    entity.ClassName,
                    model.ResultSetTypes.Count));
                valid = false;
            }

            if (model.ReturnKind != StoredProcedureReturnKind.MultiResult2)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.StoredProcedureReturnTypeMismatch,
                    location,
                    method.Name,
                    entity.ClassName));
                valid = false;
            }
        }
        else if (model.HasOutputParameters)
        {
            if (model.ReturnKind is not StoredProcedureReturnKind.ProcResult1
                and not StoredProcedureReturnKind.ProcResult2)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.StoredProcedureReturnTypeMismatch,
                    location,
                    method.Name,
                    entity.ClassName));
                valid = false;
            }
            else if (model.ReturnKind == StoredProcedureReturnKind.ProcResult1)
            {
                var outCount = model.OutParameterNames.Count;
                var validOutCount = outCount == 1
                    || (outCount == 0 && !string.IsNullOrEmpty(model.ReturnParameterName));
                if (!validOutCount)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.StoredProcedureOutParameterCountMismatch,
                        location,
                        method.Name,
                        entity.ClassName,
                        1,
                        outCount));
                    valid = false;
                }
            }
            else if (model.ReturnKind == StoredProcedureReturnKind.ProcResult2
                     && model.OutParameterNames.Count != 2)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.StoredProcedureOutParameterCountMismatch,
                    location,
                    method.Name,
                    entity.ClassName,
                    2,
                    model.OutParameterNames.Count));
                valid = false;
            }
        }
        else if (model.ReturnKind is not StoredProcedureReturnKind.EntityEnumerable
                 and not StoredProcedureReturnKind.Void)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.StoredProcedureReturnTypeMismatch,
                location,
                method.Name,
                entity.ClassName));
            valid = false;
        }

        if (provider == "Sqlite" && model.HasMultipleResultSets)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MultipleResultSetsNotSupportedOnSqlite,
                location,
                method.Name,
                entity.ClassName));
            valid = false;
        }

        return valid;
    }
}
