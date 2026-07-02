namespace Dapper.Npa.Generator.Utils;

using Microsoft.CodeAnalysis;

internal static class CascadeHelper
{
    public const int None = 0;
    public const int Persist = 1;
    public const int Merge = 2;
    public const int Remove = 4;

    public static int ParseFromRelationshipAttribute(AttributeData? attr)
    {
        if (attr is null)
            return None;

        foreach (var kvp in attr.NamedArguments)
        {
            if (kvp.Key != "Cascade")
                continue;

            return kvp.Value.Value switch
            {
                int i => i,
                null => None,
                _ => None,
            };
        }

        return None;
    }

    public static bool HasPersist(int flags) => (flags & Persist) != 0;

    public static bool HasMerge(int flags) => (flags & Merge) != 0;

    public static bool HasRemove(int flags) => (flags & Remove) != 0;
}
