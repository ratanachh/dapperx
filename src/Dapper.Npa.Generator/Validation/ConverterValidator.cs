using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;

namespace Dapper.Npa.Generator.Validation;

using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class ConverterValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, Compilation compilation, SourceProductionContext ctx)
    {
        foreach (var prop in entity.Properties.Where(p => p.ConverterTypeName is not null && !p.IsTransient))
        {
            if (prop.ConverterColumnClrTypeName is not null
                && (prop.ConverterTypeName.Contains("EnumToStringConverter", StringComparison.Ordinal)
                    || prop.ConverterTypeName.Contains("EnumToIntConverter", StringComparison.Ordinal)))
                continue;

            var propSymbol = ResolvePropertySymbol(symbol, prop);
            var location = propSymbol?.Locations.FirstOrDefault() ?? symbol.Locations.FirstOrDefault();

            if (!TryResolveConverterType(compilation, prop.ConverterTypeName, out var converterType))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InvalidConverterType, location, prop.PropertyName, entity.ClassName,
                    prop.ConverterTypeName, prop.ClrTypeName));
                continue;
            }

            if (converterType.InstanceConstructors.All(c => c.Parameters.Length > 0))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InvalidConverterType, location, prop.PropertyName, entity.ClassName,
                    prop.ConverterTypeName, prop.ClrTypeName));
                continue;
            }

            var iface = converterType.AllInterfaces.FirstOrDefault(i =>
                i.Name == "IValueConverter" && i.TypeArguments.Length == 2);
            if (iface is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InvalidConverterType, location, prop.PropertyName, entity.ClassName,
                    prop.ConverterTypeName, prop.ClrTypeName));
                continue;
            }

            var propertyType = propSymbol?.Type ?? compilation.GetTypeByMetadataName(Normalize(prop.ClrTypeName));
            if (propertyType is not null
                && !SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], propertyType))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InvalidConverterType, location, prop.PropertyName, entity.ClassName,
                    prop.ConverterTypeName, prop.ClrTypeName));
            }
        }
    }

    private static IPropertySymbol? ResolvePropertySymbol(INamedTypeSymbol entity, PropertyModel prop)
    {
        if (prop.IsEmbeddedColumn)
            return null;
        return entity.GetMembers(prop.PropertyName).OfType<IPropertySymbol>().FirstOrDefault();
    }

    private static bool TryResolveConverterType(Compilation compilation, string typeName, out INamedTypeSymbol converterType)
    {
        var resolved = compilation.GetTypeByMetadataName(typeName)
            ?? compilation.GetTypeByMetadataName(Normalize(typeName));
        converterType = resolved!;
        return resolved is not null;
    }

    private static string Normalize(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring("global::".Length) : fqn;
}
