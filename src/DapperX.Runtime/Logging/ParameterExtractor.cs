namespace DapperX.Runtime.Logging;

using System.Collections;
using System.Reflection;
using Dapper;

/// <summary>Extracts name→value pairs from Dapper parameter objects for structured logging.</summary>
public static class ParameterExtractor
{
    public static IReadOnlyDictionary<string, object?>? Extract(object? param)
    {
        if (param is null)
            return null;

        if (param is IReadOnlyDictionary<string, object?> readOnly)
            return readOnly;

        if (param is IDictionary<string, object?> dict)
            return new Dictionary<string, object?>(dict, StringComparer.Ordinal);

        if (param is DynamicParameters dynamic)
            return ExtractDynamicParameters(dynamic);

        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        var type = param.GetType();

        if (type.FullName?.StartsWith("<>", StringComparison.Ordinal) == true
            || type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is not null)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;
                result[prop.Name] = prop.GetValue(param);
            }

            return result.Count > 0 ? result : null;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;
            result[prop.Name] = prop.GetValue(param);
        }

        return result.Count > 0 ? result : null;
    }

    private static IReadOnlyDictionary<string, object?> ExtractDynamicParameters(DynamicParameters dynamic)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var name in dynamic.ParameterNames)
            result[name] = dynamic.Get<object?>(name);
        return result;
    }
}
