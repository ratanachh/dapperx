using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;

namespace Dapper.Npa.Generator.Validation;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class GlobalFilterValidator
{
    private static readonly Regex ParamNameRegex = new(@"@([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        var paramToFilters = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var filter in entity.GlobalFilters)
        {
            if (string.IsNullOrWhiteSpace(filter.Condition))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.EmptyGlobalFilterCondition,
                    symbol.Locations.FirstOrDefault(),
                    filter.Name,
                    entity.ClassName));
                continue;
            }

            foreach (Match match in ParamNameRegex.Matches(filter.Condition))
            {
                var param = match.Groups[1].Value;
                if (!paramToFilters.TryGetValue(param, out var filters))
                {
                    filters = [];
                    paramToFilters[param] = filters;
                }
                if (!filters.Contains(filter.Name, StringComparer.Ordinal))
                    filters.Add(filter.Name);
            }
        }

        foreach (var entry in paramToFilters)
        {
            if (entry.Value.Count < 2)
                continue;
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.GlobalFilterParameterConflict,
                symbol.Locations.FirstOrDefault(),
                entry.Key,
                entity.ClassName,
                string.Join(", ", entry.Value)));
        }
    }
}
