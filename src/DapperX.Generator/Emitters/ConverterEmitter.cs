namespace DapperX.Generator.Emitters;

using System.Linq;
using System.Text;
using DapperX.Generator.Models;
using Microsoft.CodeAnalysis;

internal static class ConverterEmitter
{
    public static IReadOnlyList<PropertyModel> GetConverterProperties(EntityModel entity)
        => entity.Properties.Where(p => p.ConverterTypeName is not null && !p.IsTransient).ToList();

    public static void EmitConverterFields(StringBuilder sb, EntityModel entity)
    {
        foreach (var p in GetConverterProperties(entity))
            sb.AppendLine($"    private static readonly {p.ConverterTypeName} _conv_{p.PropertyName} = new();");
        if (GetConverterProperties(entity).Count > 0)
            sb.AppendLine();
    }

    public static void EmitApplyConvertersRead(StringBuilder sb, EntityModel entity, string entityVar, string rowVar)
    {
        foreach (var p in GetConverterProperties(entity))
        {
            if (p.IsEmbeddedColumn)
            {
                var owner = p.EmbeddedOwner!;
                var inner = p.EmbeddedInner!;
                sb.AppendLine($"        if ({entityVar}.{owner} is not null)");
                sb.AppendLine($"            {entityVar}.{owner}.{inner} = DbExecutor.ConvertToProperty(_conv_{p.PropertyName}.ToProperty, {rowVar}.{p.PropertyName}, \"{p.PropertyName}\");");
            }
            else
                sb.AppendLine($"        {entityVar}.{p.PropertyName} = DbExecutor.ConvertToProperty(_conv_{p.PropertyName}.ToProperty, {rowVar}.{p.PropertyName}, \"{p.PropertyName}\");");
        }
    }

    public static string GetDbRowClrType(PropertyModel property, Compilation compilation)
    {
        if (property.ConverterColumnClrTypeName is not null)
            return property.ConverterColumnClrTypeName;
        if (property.ConverterTypeName is null)
            return property.ClrTypeName;

        var columnType = ResolveConverterColumnType(property, compilation);
        return columnType ?? property.ClrTypeName;
    }

    public static string? ResolveConverterColumnType(PropertyModel property, Compilation compilation)
    {
        if (property.ConverterTypeName is null)
            return null;

        var converterName = property.ConverterTypeName;
        INamedTypeSymbol? converterType = compilation.GetTypeByMetadataName(converterName)
            ?? compilation.GetTypeByMetadataName(NormalizeMetadataName(converterName));

        if (converterType is null)
            return null;

        var iface = converterType.AllInterfaces.FirstOrDefault(i =>
            i.Name == "IValueConverter" && i.TypeArguments.Length == 2);
        iface ??= converterType.Interfaces.FirstOrDefault(i =>
            i.Name == "IValueConverter" && i.TypeArguments.Length == 2);

        return iface?.TypeArguments[1].ToDisplayString();
    }

    public static void EmitMutationParameterEntry(
        StringBuilder sb,
        PropertyModel p,
        EntityModel entity,
        string entityVar,
        Compilation compilation,
        bool trailingComma)
    {
        var paramName = p.PropertyName;
        if (p.IsEmbeddedColumn)
        {
            var owner = p.EmbeddedOwner!;
            var inner = p.EmbeddedInner!;
            var access = $"{entityVar}.{owner}?.{inner}";
            if (p.ConverterTypeName is not null)
                sb.AppendLine($"            {paramName} = DbExecutor.ConvertToColumn(_conv_{paramName}.ToColumn, {access}!, \"{paramName}\"),");
            else
                sb.AppendLine($"            {paramName} = {access},");
        }
        else if (p.ConverterTypeName is not null)
            sb.AppendLine($"            {paramName} = DbExecutor.ConvertToColumn(_conv_{paramName}.ToColumn, {entityVar}.{paramName}, \"{paramName}\"),");
        else
            sb.AppendLine($"            {paramName} = {entityVar}.{paramName},");
    }

    private static string NormalizeMetadataName(string fqn)
    {
        var name = fqn.StartsWith("global::", StringComparison.Ordinal)
            ? fqn.Substring("global::".Length)
            : fqn;
        return name.Replace('<', '`').Replace('>', '`');
    }
}
