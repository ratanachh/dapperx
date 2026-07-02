namespace Dapper.Npa.Generator.Utils;

using Microsoft.CodeAnalysis;

/// <summary>Resolves compile-time provider: MSBuild property, then assembly attribute, then SqlServer default.</summary>
internal static class CompileTimeDatabaseProvider
{
    private const string MsBuildPropertyName = "build_property.DapperXDatabaseProvider";
    private const string AttributeClassName = "DapperXDatabaseProviderAttribute";

    public static string Resolve(Compilation compilation, string? msBuildProvider = null)
    {
        if (!string.IsNullOrWhiteSpace(msBuildProvider))
        {
            var fromBuild = Normalize(msBuildProvider);
            if (fromBuild is not null)
                return fromBuild;
        }

        foreach (var assembly in EnumerateAssemblies(compilation))
        {
            foreach (var attr in assembly.GetAttributes())
            {
                if (attr.AttributeClass?.Name != AttributeClassName)
                    continue;

                if (attr.ConstructorArguments.Length == 0)
                    continue;

                var fromEnum = MapEnumConstant(attr.ConstructorArguments[0]);
                if (fromEnum is not null)
                    return fromEnum;
            }
        }

        return "SqlServer";
    }

    private static IEnumerable<IAssemblySymbol> EnumerateAssemblies(Compilation compilation)
    {
        yield return compilation.Assembly;
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol asm)
                yield return asm;
        }
    }

    private static string? MapEnumConstant(TypedConstant constant)
    {
        if (constant.Value is not int i)
            return null;

        return i switch
        {
            0 => "SqlServer",
            1 => "PostgreSql",
            2 => "MySql",
            3 => "Sqlite",
            _ => null,
        };
    }

    private static string? Normalize(string raw)
    {
        var trimmed = raw.Trim();
        return trimmed switch
        {
            "SqlServer" or "SQLServer" or "MSSQL" => "SqlServer",
            "PostgreSql" or "PostgreSQL" or "Postgres" or "Npgsql" => "PostgreSql",
            "MySql" or "MySQL" or "MariaDB" => "MySql",
            "Sqlite" or "SQLite" => "Sqlite",
            _ => null,
        };
    }
}
