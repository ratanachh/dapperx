namespace Dapper.Npa.Query.Sql;
public static class SqlParameterBuilder
{
    public static object Merge(params object?[] paramObjects)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var obj in paramObjects)
        {
            if (obj is null) continue;
            foreach (var prop in obj.GetType().GetProperties())
                dict[prop.Name] = prop.GetValue(obj);
        }
        return dict;
    }
}
