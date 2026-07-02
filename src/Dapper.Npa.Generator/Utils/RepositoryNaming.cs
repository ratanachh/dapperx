namespace Dapper.Npa.Generator.Utils;

internal static class RepositoryNaming
{
    /// <summary>
    /// Strips leading 'I', appends 'Impl'. E.g. IProductRepository → ProductRepositoryImpl.
    /// </summary>
    public static string DeriveImplClassName(string interfaceName)
    {
        var name = interfaceName.StartsWith("I", StringComparison.Ordinal) && interfaceName.Length > 1
            ? interfaceName.Substring(1)
            : interfaceName;
        return name + "Impl";
    }

    public static string DefaultImplClassName(string entityClassName)
        => $"{entityClassName}RepositoryImpl";
}
