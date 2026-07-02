namespace Dapper.Npa.Generator.Utils;

internal static class TypeCompatibilityHelper
{
    private const string GlobalPrefix = "global::";

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["int"] = "System.Int32",
        ["long"] = "System.Int64",
        ["short"] = "System.Int16",
        ["byte"] = "System.Byte",
        ["bool"] = "System.Boolean",
        ["string"] = "System.String",
        ["char"] = "System.Char",
        ["decimal"] = "System.Decimal",
        ["double"] = "System.Double",
        ["float"] = "System.Single",
        ["object"] = "System.Object",
        ["uint"] = "System.UInt32",
        ["ulong"] = "System.UInt64",
        ["ushort"] = "System.UInt16",
        ["sbyte"] = "System.SByte",
    };

    public static bool AreCompatible(string? expected, string? actual)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            return false;

        var normalizedExpected = Normalize(expected);
        var normalizedActual = Normalize(actual);
        if (string.Equals(normalizedExpected, normalizedActual, StringComparison.Ordinal))
            return true;

        return string.Equals(GetShortName(normalizedExpected), GetShortName(normalizedActual), StringComparison.Ordinal);
    }

    public static string Normalize(string typeName)
    {
        var normalized = typeName.Trim();
        if (normalized.StartsWith(GlobalPrefix, StringComparison.Ordinal))
            normalized = normalized.Substring(GlobalPrefix.Length);

        if (normalized.EndsWith("?", StringComparison.Ordinal))
            normalized = normalized.Substring(0, normalized.Length - 1);

        return Aliases.TryGetValue(normalized, out var alias) ? alias : normalized;
    }

    private static string GetShortName(string normalizedTypeName)
    {
        var lastDot = normalizedTypeName.LastIndexOf('.');
        return lastDot >= 0 ? normalizedTypeName.Substring(lastDot + 1) : normalizedTypeName;
    }
}
